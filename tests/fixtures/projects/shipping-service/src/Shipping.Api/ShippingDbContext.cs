using Microsoft.EntityFrameworkCore;

namespace Shipping.Api;

public sealed record Shipment(Guid Id, string Destination, string Carrier);

public sealed class ShippingDbContext(DbContextOptions<ShippingDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
}
