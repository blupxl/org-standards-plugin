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

// Server instructions reach every agent in a session (planning skills from other plugins too), so
// the company's decisions are known before anything is planned. Kept short: they're always loaded.
const string Instructions =
    "Company standards and approved recipes. Before planning or building in a project, read " +
    ".claude/standards.json if it exists (each component's scope, its choices, approved exclusions) " +
    "and call get_standards for the components the work touches. To work out a scope, classify the work " +
    "against get_taxonomy's categories and signals. For a new project or a new capability " +
    "(data access, messaging, ...), list the approved recipes with get_standards and the filter " +
    "{ \"template\": [\"recipe\"], \"kind\": [...], \"runtime\": [...], \"uses\": [...] }, prefer the one marked " +
    "recommended unless the user picks another, fetch it with get_topic, and record the choice in " +
    ".claude/standards.json. When a plan is finished and before implementing it, check it against the " +
    "standards (the plugin's plan-check skill). Never add exclusions of your own.";

builder.Services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "org-standards", Version = "0.1.0" };
        options.ServerInstructions = Instructions;
    })
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
