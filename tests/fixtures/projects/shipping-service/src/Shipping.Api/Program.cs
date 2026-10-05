using Microsoft.EntityFrameworkCore;
using Shipping.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShippingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("shipping")));

var app = builder.Build();

app.MapGet("/shipments/{id:guid}", async (Guid id, ShippingDbContext db) =>
    await db.Shipments.FindAsync(id) is { } shipment ? Results.Ok(shipment) : Results.NotFound());

app.Run();
