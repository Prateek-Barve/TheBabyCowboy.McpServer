
// This is the entry point for the TheBabyCowboy.McpServer application.

// Package references:
using ModelContextProtocol.AspNetCore;
using TheBabyCowboy.McpServer.Tools;

// Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// API URL Definition and Validation
// This helper reads an API URL from configuration and ensures that it is a valid HTTPS URL.
static Uri GetApiBaseUri(
    IConfiguration configuration,
    string settingName)
{
    var url = configuration[settingName];

    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
        uri.Scheme != Uri.UriSchemeHttps)
    {
        throw new InvalidOperationException(
            $"{settingName} must be a valid HTTPS URL.");
    }

    return uri;
}

// Configure a reusable HttpClient with the API's base URL, timeout, and default request headers.
static void ConfigureApiClient(
    HttpClient client,
    Uri baseUri)
{
    client.BaseAddress = baseUri;
    client.Timeout = TimeSpan.FromSeconds(15);

    client.DefaultRequestHeaders.Accept.ParseAdd(
        "application/json");
}

// Content API URL Definition
// Read and validate the Content API URL from configuration.
var contentApiUri = GetApiBaseUri(
    builder.Configuration,
    "PortfolioApis:ContentApiBaseUrl");

// Media API URL Definition
// Read and validate the Media API URL from configuration.
var mediaApiUri = GetApiBaseUri(
    builder.Configuration,
    "PortfolioApis:MediaApiBaseUrl");

// Home API URL Definition
// Read and validate the Home API URL from configuration.
var homeApiUri = GetApiBaseUri(
    builder.Configuration,
    "PortfolioApis:HomeApiBaseUrl");

// Add the PortfolioTools service's named HttpClients.
// Named clients are used because PortfolioTools accesses multiple existing APIs.
// Each client is configured with its respective API base URL, timeout, and default request headers.
builder.Services.AddHttpClient("ContentApi", client =>
    ConfigureApiClient(client, contentApiUri));

builder.Services.AddHttpClient("MediaApi", client =>
    ConfigureApiClient(client, mediaApiUri));

builder.Services.AddHttpClient("HomeApi", client =>
    ConfigureApiClient(client, homeApiUri));

// Adding / Registering Services

// Add the MCP server with HTTP transport and tools.
// The MCP server is configured to use stateless sessions and includes the PortfolioTools.
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithTools<ContentTools>()
    .WithTools<MediaTools>()
    .WithTools<HomeTools>()
    .WithTools<WebTools>();

// Add CORS policy to allow requests from specific origins.
// This policy allows the local MCP Inspector frontend to communicate with the MCP server.
builder.Services.AddCors(options =>
{
    options.AddPolicy("McpInspectorCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:6274",
                "http://127.0.0.1:6274")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(
                "Mcp-Session-Id",
                "Mcp-Protocol-Version");
    });
});

// Build the web application
var app = builder.Build();

// Configure the HTTP request pipeline for MCP Inspector CORS policy.
app.UseCors("McpInspectorCors");

// Map the root endpoint to return a simple status message.
// This endpoint returns a JSON object indicating the service name and its running status.
app.MapGet("/", () => Results.Ok(new
{
    service = "TheBabyCowboy.McpServer",
    status = "running"
}));

// Map the MCP endpoint to handle MCP requests.
// MCP clients connect to this endpoint to discover and execute the registered tools.
app.MapMcp("/mcp");

// Run the web application
app.Run();