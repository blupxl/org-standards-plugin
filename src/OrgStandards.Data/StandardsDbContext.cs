using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using OrgStandards.Contracts;

namespace OrgStandards.Data;

public sealed class Topic
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Document { get; set; }
    public required string Version { get; set; }
    public required string Body { get; set; }
    public List<TopicDetail> Details { get; set; } = [];
    public List<TopicTag> Tags { get; set; } = [];
}

public sealed class TopicTag
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public required string Field { get; set; }
    public required string Value { get; set; }
}

public sealed class StandardsDbContext(DbContextOptions<StandardsDbContext> options) : DbContext(options)
{
    public DbSet<Topic> Topics => Set<Topic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Topic>(topic =>
        {
            topic.Property(t => t.Name).HasMaxLength(200);
            topic.Property(t => t.Document).HasMaxLength(200);
            topic.Property(t => t.Version).HasMaxLength(50);
            topic.HasIndex(t => t.Name);
            topic.HasMany(t => t.Tags).WithOne().HasForeignKey(t => t.TopicId).OnDelete(DeleteBehavior.Cascade);

            // Free-form extras (examples, reference tables) as JSON, so authors can add content
            // without a schema change.
            topic.Property(t => t.Details)
                .HasColumnType("jsonb")
                .HasConversion(
                    details => JsonSerializer.Serialize(details, Json.Options),
                    json => JsonSerializer.Deserialize<List<TopicDetail>>(json, Json.Options) ?? new List<TopicDetail>(),
                    new ValueComparer<List<TopicDetail>>(
                        (left, right) => left!.SequenceEqual(right!),
                        details => details.Aggregate(0, (hash, detail) => HashCode.Combine(hash, detail.GetHashCode())),
                        details => details.ToList()));
        });

        modelBuilder.Entity<TopicTag>(tag =>
        {
            tag.Property(t => t.Field).HasMaxLength(100);
            tag.Property(t => t.Value).HasMaxLength(200);
            tag.HasIndex(t => new { t.Field, t.Value });
        });
    }
}

// Lets `dotnet ef migrations add` build the model without a running database.
public sealed class DesignTimeStandardsDbContextFactory : IDesignTimeDbContextFactory<StandardsDbContext>
{
    public StandardsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<StandardsDbContext>().UseNpgsql("Host=localhost;Database=design_time").Options);
}
