using OrgStandards.Data;
using OrgStandards.Source;

// A downstream MCP server that owns one set of standards (e.g. engineering, branding).
// The same project runs once per owner; Aspire passes the owner's name and database.
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var sourceName = builder.Configuration["Source:Name"]
    ?? throw new InvalidOperationException(
        "OrgStandards.Source is started by the Aspire AppHost, which gives each instance its name and database. " +
        "Run OrgStandards.AppHost instead (in Visual Studio: set it as the startup project).");

builder.AddNpgsqlDbContext<StandardsDbContext>($"{sourceName}-db");
// Resources this owner points to, e.g. Links:design-system = http://localhost:5500. The AppHost
// sets them; standards refer to them as {{design-system}} and never hard-code an address.
var links = builder.Configuration.GetSection("Links").GetChildren()
    .Where(link => !string.IsNullOrWhiteSpace(link.Value))
    .ToDictionary(link => link.Key, link => link.Value!.TrimEnd('/'), StringComparer.OrdinalIgnoreCase);

builder.Services.AddSingleton(new SourceInfo(sourceName, links));
builder.Services.AddMcpServer(options => options.ServerInfo = new() { Name = $"standards-{sourceName}", Version = "0.1.0" })
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapMcp("/mcp");
app.Run();
