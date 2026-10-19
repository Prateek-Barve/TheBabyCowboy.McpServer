// MCP tools for publicly available data from TheBabyCowboy Content API.
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace TheBabyCowboy.McpServer.Tools;

// Groups tools related to projects, skills, certifications, blogs, journals and travel.
[McpServerToolType]
public sealed class ContentTools(IHttpClientFactory httpClientFactory)
{
    // Retrieves all publicly available portfolio projects.
    [McpServerTool]
    [Description("Retrieves all publicly available projects from TheBabyCowboy portfolio.")]
    public Task<string> GetProjects(CancellationToken cancellationToken = default) =>
        PortfolioToolHelpers.GetAsync(httpClientFactory, "ContentApi", "api/Projects", cancellationToken);

    // Retrieves one project using its GUID.
    [McpServerTool]
    [Description("Retrieves the details of a specific portfolio project using its GUID.")]
    public Task<string> GetProjectDetails(
        [Description("The GUID of the project.")] string id,
        CancellationToken cancellationToken = default)
    {
        // Validate the ID before constructing the API request.
        if (!Guid.TryParse(id, out var projectId))
            throw new ArgumentException("A valid project GUID is required.", nameof(id));

        return PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", $"api/Projects/{projectId:D}", cancellationToken);
    }

    // Searches project data locally by matching text in returned project JSON.
    [McpServerTool]
    [Description("Searches portfolio projects by title, descriptions, skills or other project details.")]
    public async Task<string> SearchProjects(
        [Description("Text to search for, such as React, Azure or inventory.")] string searchTerm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("A search term is required.", nameof(searchTerm));

        var json = await PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", "api/Projects", cancellationToken);

        return PortfolioToolHelpers.FilterCollection(json, searchTerm.Trim());
    }

    // Retrieves skills, optionally filtered by category GUID.
    [McpServerTool]
    [Description("Retrieves portfolio skills, optionally filtered by skill category GUID.")]
    public Task<string> GetSkills(
        [Description("Optional skill category GUID.")] string? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = PortfolioToolHelpers.BuildQuery(("categoryId", categoryId));
        return PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", $"api/skills{query}", cancellationToken);
    }

    // Retrieves available skill categories.
    [McpServerTool]
    [Description("Retrieves the available skill categories in the portfolio.")]
    public Task<string> GetSkillCategories(CancellationToken cancellationToken = default) =>
        PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", "api/skill-categories", cancellationToken);

    // Retrieves portfolio certifications, issuers and URLs.
    [McpServerTool]
    [Description("Retrieves portfolio certifications and their issuers and URLs.")]
    public Task<string> GetCertifications(CancellationToken cancellationToken = default) =>
        PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", "api/certifications", cancellationToken);

    // Retrieves published portfolio blog posts.
    [McpServerTool]
    [Description("Retrieves published portfolio blog posts.")]
    public Task<string> GetBlogs(CancellationToken cancellationToken = default) =>
        PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", "api/blogs", cancellationToken);

    // Searches only the public journal endpoint; private journal endpoints are not exposed.
    // Supports text, mood, category, tag and date-range filters.
    [McpServerTool]
    [Description("Searches publicly available journal entries by text, mood, category, tag or date range. Never retrieves private journals.")]
    public Task<string> GetPublicJournals(
        [Description("Optional text search.")] string? search = null,
        [Description("Optional mood filter.")] string? mood = null,
        [Description("Optional category filter.")] string? category = null,
        [Description("Optional tag filter.")] string? tag = null,
        [Description("Optional start date filter.")] string? from = null,
        [Description("Optional end date filter.")] string? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = PortfolioToolHelpers.BuildQuery(
            ("search", search), ("mood", mood), ("category", category),
            ("tag", tag), ("from", from), ("to", to));

        return PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", $"api/journals/public{query}", cancellationToken);
    }

    // Retrieves public travel trips and their details.
    [McpServerTool]
    [Description("Retrieves travel trips and publicly available travel information from the portfolio.")]
    public Task<string> GetTravelTrips(CancellationToken cancellationToken = default) =>
        PortfolioToolHelpers.GetAsync(
            httpClientFactory, "ContentApi", "api/travel", cancellationToken);
}
