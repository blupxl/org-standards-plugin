using OrgStandards.Migrations;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<SeedWorker>();

builder.Build().Run();
