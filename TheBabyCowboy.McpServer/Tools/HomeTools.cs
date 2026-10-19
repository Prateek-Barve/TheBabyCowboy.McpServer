// MCP tools for publicly available data from TheBabyCowboy Home API.
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TheBabyCowboy.McpServer.Tools;

// Groups read-only tools related to the Home API.
[McpServerToolType]
public sealed class HomeTools(IHttpClientFactory httpClientFactory)
{
    // Retrieves a random public thought. This does not create or retrieve visitor reflections.
    [McpServerTool]
    [Description("Retrieves a random public thought from TheBabyCowboy Home API.")]
    public Task<string> GetRandomThought(CancellationToken cancellationToken = default) =>
        PortfolioToolHelpers.GetAsync(
            httpClientFactory, "HomeApi", "api/Thoughts/random", cancellationToken);
}
