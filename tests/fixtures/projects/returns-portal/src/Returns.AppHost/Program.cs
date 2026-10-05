var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var cache = builder.AddRedis("cache");

builder.AddProject<Projects.Returns_Api>("returns-api")
    .WithReference(postgres)
    .WithReference(cache);

builder.Build().Run();
