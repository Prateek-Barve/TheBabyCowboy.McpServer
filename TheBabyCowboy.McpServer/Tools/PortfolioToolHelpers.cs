// Shared helper methods used by the domain-specific MCP tool classes.
// This keeps HTTP, query-string and JSON-search logic in one place.
using System.Text.Json;

namespace TheBabyCowboy.McpServer.Tools;

internal static class PortfolioToolHelpers
{
    // Maximum response size returned by a tool to avoid excessively large outputs.
    private const int MaxCharacters = 20_000;

    // Calls a named API client, checks the HTTP status and returns the response body.
    internal static async Task<string> GetAsync(
        IHttpClientFactory httpClientFactory,
        string clientName,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        // Create the configured HttpClient for the requested API.
        using var client = httpClientFactory.CreateClient(clientName);

        // Send a GET request to the API endpoint.
        using var response = await client.GetAsync(relativeUrl, cancellationToken);

        // Throw if the API returns an unsuccessful HTTP status code.
        response.EnsureSuccessStatusCode();

        // Read the API response body, usually JSON.
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        // Limit the response size. Character truncation may make large JSON incomplete.
        if (content.Length > MaxCharacters)
            content = content[..MaxCharacters] + "\n[Response truncated]";

        return content;
    }

    // Builds a query string, omitting null, empty and whitespace values.
    // URL-encoding handles spaces and special characters safely.
    internal static string BuildQuery(params (string Key, string? Value)[] parameters)
    {
        var values = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");

        var query = string.Join("&", values);
        return string.IsNullOrEmpty(query) ? "" : $"?{query}";
    }

    // Supports either a JSON array or a paginated object containing an "items" array.
    internal static JsonElement GetItemsArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root;

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.Equals("items", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.Array)
                    return property.Value;
            }
        }

        throw new InvalidOperationException(
            "The API response does not contain a supported collection.");
    }

    // Searches string values recursively, including nested objects and arrays.
    internal static bool ContainsSearchTerm(JsonElement element, string searchTerm)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString()?.Contains(
                    searchTerm, StringComparison.OrdinalIgnoreCase) == true;
            case JsonValueKind.Object:
                return element.EnumerateObject().Any(p =>
                    ContainsSearchTerm(p.Value, searchTerm));
            case JsonValueKind.Array:
                return element.EnumerateArray().Any(item =>
                    ContainsSearchTerm(item, searchTerm));
            default:
                return false;
        }
    }

    // Filters a collection and returns up to 20 matching records as structured JSON.
    internal static string FilterCollection(string json, string searchTerm)
    {
        using var document = JsonDocument.Parse(json);
        var items = GetItemsArray(document.RootElement);

        // Clone matches so they remain usable after the JsonDocument is disposed.
        var results = items.EnumerateArray()
            .Where(item => ContainsSearchTerm(item, searchTerm))
            .Take(20)
            .Select(item => item.Clone())
            .ToList();

        return JsonSerializer.Serialize(new
        {
            query = searchTerm,
            count = results.Count,
            results
        });
    }
}
