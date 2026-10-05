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
            topic.Property(entity => entity.Name).HasMaxLength(200);
            topic.Property(entity => entity.Document).HasMaxLength(200);
            topic.Property(entity => entity.Version).HasMaxLength(50);
            topic.HasIndex(entity => entity.Name);
            topic.HasMany(entity => entity.Tags).WithOne().HasForeignKey(entity => entity.TopicId).OnDelete(DeleteBehavior.Cascade);

            // Free-form extras (examples, reference tables) as JSON, so authors can add content
            // without a schema change.
            topic.Property(entity => entity.Details)
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
            tag.Property(entity => entity.Field).HasMaxLength(100);
            tag.Property(entity => entity.Value).HasMaxLength(200);
            tag.HasIndex(entity => new { entity.Field, entity.Value });
        });
    }
}

// Lets `dotnet ef migrations add` build the model without a running database.
public sealed class DesignTimeStandardsDbContextFactory : IDesignTimeDbContextFactory<StandardsDbContext>
{
    public StandardsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<StandardsDbContext>().UseNpgsql("Host=localhost;Database=design_time").Options);
}
