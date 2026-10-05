var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql");
var mongo = builder.AddMongoDB("mongo");
var kafka = builder.AddKafka("kafka");
// var cache = builder.AddRedis("cache");

builder.AddProject<Projects.Billing_Worker>("billing-worker")
    .WithReference(sql)
    .WithReference(mongo)
    .WithReference(kafka);

builder.Build().Run();
