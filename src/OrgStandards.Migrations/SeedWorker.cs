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
        catch (Exception exception)
        {
            logger.LogError(exception, "Migration or seeding failed");
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

        await using var database = new StandardsDbContext(
            new DbContextOptionsBuilder<StandardsDbContext>().UseNpgsql(connectionString).Options);

        await database.Database.MigrateAsync(cancellationToken);

        var folder = Path.Combine(AppContext.BaseDirectory, "seed", source);
        var topics = Directory.EnumerateFiles(folder, "*.md").Order().SelectMany(StandardsMarkdown.Parse).ToList();

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await database.Topics.ExecuteDeleteAsync(cancellationToken);
        database.Topics.AddRange(topics);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} topics into {Source}", topics.Count, source);
    }
}
