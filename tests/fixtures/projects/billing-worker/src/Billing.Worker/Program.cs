using Billing.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<InvoiceWorker>();

builder.Build().Run();
