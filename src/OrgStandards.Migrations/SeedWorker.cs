using Microsoft.EntityFrameworkCore;
using OrgStandards.Data;

namespace OrgStandards.Migrations;

// Runs once at startup: migrates each source's database, then replaces its contents with the
// Markdown documents in seed/<source>/. The files are the source of truth, so edits show up on the
// next run.
// Exits non-zero on failure so the sources (which wait for completion) never start on bad data.
public sealed class SeedWorker(
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<SeedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            foreach (var source in configuration.GetSection("Seed:Sources").Get<string[]>() ?? [])
            {
                await SeedAsync(source, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration or seeding failed");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task SeedAsync(string source, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString($"{source}-db")
            ?? throw new InvalidOperationException($"No connection string for source '{source}'.");

        await using var db = new StandardsDbContext(
            new DbContextOptionsBuilder<StandardsDbContext>().UseNpgsql(connectionString).Options);

        await db.Database.MigrateAsync(cancellationToken);

        var folder = Path.Combine(AppContext.BaseDirectory, "seed", source);
        var topics = Directory.EnumerateFiles(folder, "*.md").Order().SelectMany(StandardsMarkdown.Parse).ToList();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Topics.ExecuteDeleteAsync(cancellationToken);
        db.Topics.AddRange(topics);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} topics into {Source}", topics.Count, source);
    }
}
