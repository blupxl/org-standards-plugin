using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgWeb();

// Acme's design-system site (stand-in): the source of truth for the stylesheet the design
// standards require. Fixed at http://localhost:5500 so saved pages keep working between runs.
var acmeWeb = builder.AddProject<Projects.Acme_Web>("acme-web")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Acme design system");

// Resources an owner points to. Its standards write {{name}}; its server fills in the address.
var links = new Dictionary<string, (string Name, IResourceBuilder<ProjectResource> Target)[]>
{
    ["design"] = [("design-system", acmeWeb)],
};

// The downstream MCP servers that own the standards: the fan-out chain.
// To add one, add its name here and a folder of standards at OrgStandards.Migrations/seed/<name>/.
string[] sources = ["design", "platform", "security", "data"];

// The owner that serves the taxonomy (the architecture group's): get_taxonomy reads it from here.
const string TaxonomyOwner = "platform";

var migrations = builder.AddProject<Projects.OrgStandards_Migrations>("migrations")
    .WaitFor(postgres);

// Claude Code connects here: http://localhost:5480/mcp (fixed in the gateway's launchSettings.json).
// Its home page, http://localhost:5480/, is the "Getting started" guide that F5 opens.
var gateway = builder.AddProject<Projects.OrgStandards_Gateway>("gateway")
    .WithUrlForEndpoint("http", url => url.DisplayText = "Getting started")
    .WithEnvironment("Gateway__TaxonomyOwner", TaxonomyOwner);

for (var i = 0; i < sources.Length; i++)
{
    var name = sources[i];
    var database = postgres.AddDatabase($"{name}-db");

    migrations
        .WithReference(database)
        .WithEnvironment($"Seed__Sources__{i}", name);

    // One project, several instances: ignore launch profiles so each instance gets its own port.
    var source = builder.AddProject<Projects.OrgStandards_Source>(name, options => options.ExcludeLaunchProfile = true)
        .WithHttpEndpoint()
        .WithReference(database)
        .WithEnvironment("Source__Name", name)
        .WithEnvironment("Source__ServesTaxonomy", (name == TaxonomyOwner).ToString())
        // Without a launch profile, pass the environment on ourselves, and skip IDE add-ins
        // (e.g. Visual Studio's endpoint discovery) that can't load outside their launch profile.
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
        .WithEnvironment("ASPNETCORE_PREVENTHOSTINGSTARTUP", "true")
        .WaitForCompletion(migrations);

    foreach (var (linkName, target) in links.GetValueOrDefault(name, []))
    {
        source.WithEnvironment($"Links__{linkName}", target.GetEndpoint("http"));
    }

    gateway
        .WithReference(source)
        .WithEnvironment($"Gateway__Sources__{i}", name);
}

builder.Build().Run();
