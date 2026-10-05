using Returns.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<ReturnsDbContext>("returns");
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("cache"));

var app = builder.Build();

app.MapPost("/returns", async (ReturnRequest request, ReturnsDbContext db) =>
{
    db.Returns.Add(request);
    await db.SaveChangesAsync();
    return Results.Created($"/returns/{request.Id}", request);
});

app.Run();
