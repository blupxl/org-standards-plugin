using Microsoft.EntityFrameworkCore;

namespace Returns.Api;

public sealed record ReturnRequest(Guid Id, string OrderNumber, string Reason);

public sealed class ReturnsDbContext(DbContextOptions<ReturnsDbContext> options) : DbContext(options)
{
    public DbSet<ReturnRequest> Returns => Set<ReturnRequest>();
}
