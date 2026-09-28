using OrgStandards.Gateway;

// The front door Claude Code connects to. It exposes org-shaped tools and fans out to the
// downstream sources listed in Gateway:Sources (set by the AppHost).
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// No retries on source calls: an unavailable source is reported as such, quickly,
// instead of being retried behind the caller's back.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is marked experimental.
builder.Services.AddHttpClient(SourceClient.HttpClientName).RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
builder.Services.AddSingleton<SourceClient>();

builder.Services.AddMcpServer(options => options.ServerInfo = new() { Name = "org-standards", Version = "0.1.0" })
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

if (app.Services.GetRequiredService<SourceClient>().Sources.Length == 0)
{
    app.Logger.LogWarning("No standards sources configured. Run OrgStandards.AppHost, which starts the sources and wires them to the gateway.");
}

app.MapDefaultEndpoints();
app.UseDefaultFiles();
app.UseStaticFiles();   // wwwroot/index.html: the "Getting started" guide at "/"
app.MapMcp("/mcp");
app.Run();
