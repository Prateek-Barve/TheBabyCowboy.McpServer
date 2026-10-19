// MCP tools for books, movies, series and playlists from TheBabyCowboy Media API.
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace TheBabyCowboy.McpServer.Tools;

// Groups media retrieval and search tools.
[McpServerToolType]
public sealed class MediaTools(IHttpClientFactory httpClientFactory)
{
    // Retrieves media with optional category, status and genre filters and pagination.
    [McpServerTool]
    [Description("Retrieves books, movies, series and playlists. Supports category, status and genre filters with pagination.")]
    public Task<string> GetMedia(
        [Description("Optional media category filter.")] string? category = null,
        [Description("Optional status filter.")] string? status = null,
        [Description("Optional genre filter.")] string? genre = null,
        [Description("Page number, starting at 1.")] int page = 1,
        [Description("Number of items per page, maximum 50.")] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination to prevent invalid or excessively large requests.
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        if (pageSize < 1 || pageSize > 50)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 50.");

        var query = PortfolioToolHelpers.BuildQuery(
            ("category", category), ("status", status), ("genre", genre),
            ("page", page.ToString()), ("pageSize", pageSize.ToString()));

        return PortfolioToolHelpers.GetAsync(
            httpClientFactory, "MediaApi", $"api/Media{query}", cancellationToken);
    }

    // Searches up to five pages (50 records) and returns up to 20 matching media items.
    [McpServerTool]
    [Description("Searches books, movies, series and playlists by text. Supports optional category, status and genre filters. Searches up to 50 media records.")]
    public async Task<string> SearchMedia(
        [Description("Text to search for in media details.")] string searchTerm,
        [Description("Optional media category filter.")] string? category = null,
        [Description("Optional status filter.")] string? status = null,
        [Description("Optional genre filter.")] string? genre = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("A search term is required.", nameof(searchTerm));

        var normalizedSearchTerm = searchTerm.Trim();
        var matches = new List<JsonElement>();
        var pagesSearched = 0;
        var hasMorePages = false;

        // Bound the amount of work to at most five API requests.
        for (var page = 1; page <= 5; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query = PortfolioToolHelpers.BuildQuery(
                ("category", category), ("status", status), ("genre", genre),
                ("page", page.ToString()), ("pageSize", "10"));

            var json = await PortfolioToolHelpers.GetAsync(
                httpClientFactory, "MediaApi", $"api/Media{query}", cancellationToken);

            using var document = JsonDocument.Parse(json);
            var items = PortfolioToolHelpers.GetItemsArray(document.RootElement);
            pagesSearched++;

            foreach (var item in items.EnumerateArray())
            {
                if (PortfolioToolHelpers.ContainsSearchTerm(item, normalizedSearchTerm))
                {
                    // Clone each result so it survives disposal of this page's JSON document.
                    matches.Add(item.Clone());
                    if (matches.Count >= 20)
                        break;
                }
            }

            if (matches.Count >= 20)
            {
                hasMorePages = true;
                break;
            }

            // Inspect pagination metadata to decide whether another page is available.
            hasMorePages = false;
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (property.Name.Equals("hasNextPage", StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.True)
                    {
                        hasMorePages = true;
                        break;
                    }
                }
            }

            if (!hasMorePages)
                break;
        }

        // Return matches plus metadata so the caller understands the search scope.
        return JsonSerializer.Serialize(new
        {
            query = normalizedSearchTerm,
            pagesSearched,
            count = matches.Count,
            results = matches,
            mayHaveMoreResults = hasMorePages
        });
    }
}
