using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OrgStandards.Contracts;

namespace OrgStandards.Gateway;

public sealed record SourceCall<T>(string Source, T? Value, string? Error)
{
    public bool Succeeded => Error is null;
}

// Calls a tool on one downstream MCP server. A failure never throws: it comes back as a
// SourceCall with an Error, so one unavailable source can't fail the whole request.
public sealed class SourceClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILoggerFactory loggerFactory)
{
    public const string HttpClientName = "sources";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public string[] Sources => configuration.GetSection("Gateway:Sources").Get<string[]>() ?? [];

    // The one owner whose taxonomy is used (set by the AppHost); none means every owner's is merged.
    public string? TaxonomyOwner => configuration["Gateway:TaxonomyOwner"];

    public Task<SourceCall<T>[]> CallAllAsync<T>(string tool, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken) =>
        Task.WhenAll(Sources.Select(source => CallAsync<T>(source, tool, arguments, cancellationToken)));

    public async Task<SourceCall<T>> CallAsync<T>(
        string source, string tool, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            // "http://<source>" is resolved by Aspire service discovery.
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri($"http://{source}/mcp"),
                    Name = source,
                    TransportMode = HttpTransportMode.StreamableHttp,
                },
                httpClientFactory.CreateClient(HttpClientName),
                loggerFactory,
                ownsHttpClient: true);

            await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
            var result = await client.CallToolAsync(tool, arguments, cancellationToken: timeout.Token);
            var text = string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));

            if (result.IsError == true)
            {
                return new SourceCall<T>(source, default, text);
            }

            return new SourceCall<T>(source, JsonSerializer.Deserialize<T>(text, Json.Options), null);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            var reason = timeout.IsCancellationRequested ? $"timed out after {Timeout.TotalSeconds:0}s" : exception.Message;
            return new SourceCall<T>(source, default, reason);
        }
    }
}
