using System.Diagnostics;
using System.Text.RegularExpressions;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace OrgStandards.Tests.Integration;

// Starts the whole AppHost once for every integration test: Postgres (in a container),
// migrations, the owners, the gateway and Acme.Web. Ports are randomized, so a running F5 session
// doesn't get in the way.
public sealed class StandardsAppFixture : IAsyncLifetime
{
    // The first run pulls the Postgres image.
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? _app;

    private DistributedApplication App =>
        _app ?? throw new InvalidOperationException("The app isn't running; these tests need a container runtime.");

    public Uri DesignSystem => App.GetEndpoint("acme-web", "http");

    public async Task InitializeAsync()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            return;
        }

        using var timeout = new CancellationTokenSource(StartupTimeout);
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.OrgStandards_AppHost>(timeout.Token);
        _app = await builder.BuildAsync(timeout.Token);
        await _app.StartAsync(timeout.Token);

        // The owners wait for the migrations, so once they're up the standards are loaded.
        foreach (var resource in new[] { "design", "platform", "security", "data", "gateway", "acme-web" })
        {
            await _app.ResourceNotifications.WaitForResourceHealthyAsync(resource, timeout.Token);
        }

        // "Healthy" means each process is up, not that it answers quickly: an owner's first query
        // (database connection, EF Core's model) can take longer than the gateway's 10-second limit
        // on a busy machine. Wait until a discovery call gets an answer from every owner, so no test
        // depends on running first or last.
        while (!Regex.IsMatch(await CallAsync("list_standards", []), "\"status\"\\s*:\\s*\"ok\""))
        {
            await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
        }
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    public HttpClient CreateHttpClient(string resource) => App.CreateHttpClient(resource, "http");

    // Calls a gateway tool the way Claude Code does: over MCP, via HTTP.
    public async Task<string> CallAsync(string tool, Dictionary<string, object?> arguments)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(App.GetEndpoint("gateway", "http"), "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        });

        await using var client = await McpClient.CreateAsync(transport);
        var result = await client.CallToolAsync(tool, arguments);
        return string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));
    }
}

[CollectionDefinition(Name)]
public sealed class StandardsAppCollection : ICollectionFixture<StandardsAppFixture>
{
    public const string Name = "Standards app";
}

// Skips an integration test cleanly when no container runtime is running.
public sealed class RequiresContainerRuntimeFactAttribute : FactAttribute
{
    public RequiresContainerRuntimeFactAttribute()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Skip = "Needs a running container runtime (Docker, Podman, or Rancher Desktop with dockerd).";
        }
    }
}

internal static class ContainerRuntime
{
    public static readonly bool IsAvailable = Check();

    // Asks the runtime Aspire will use ("docker" unless ASPIRE_CONTAINER_RUNTIME says otherwise).
    private static bool Check()
    {
        var runtime = Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME") ?? "docker";
        try
        {
            using var process = Process.Start(new ProcessStartInfo(runtime, "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });

            if (process is null)
            {
                return false;
            }

            _ = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();
            return process.WaitForExit(TimeSpan.FromSeconds(15)) && process.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
