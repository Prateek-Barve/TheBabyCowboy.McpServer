TheBabyCowboy MCP tools refactor

Files:
- Tools/ContentTools.cs: projects, project details/search, skills, categories, certifications, blogs, public journals and travel.
- Tools/MediaTools.cs: media retrieval and search.
- Tools/HomeTools.cs: random public thought.
- Tools/PortfolioToolHelpers.cs: shared HTTP, query-string and JSON-search helpers.
- Program.cs-registration-snippet.txt: MCP tool registration to use in Program.cs.

Steps:
1. Add all four .cs files to the Tools folder.
2. Keep your existing named HttpClient registrations: ContentApi, MediaApi and HomeApi.
3. Replace .WithTools<PortfolioTools>() with the three registrations in the snippet.
4. Remove or exclude PortfolioTools.cs to avoid registering duplicate tool names.
5. Build and test tool discovery/calls in Postman.

The existing tool names and endpoint paths are preserved. Only public journal access is exposed; private journal endpoints and reflection creation are not included.

TheBabyCowboy MCP - SearXNG Web Search (No commercial API key)

WHAT THIS DOES
- Replaces the Brave/Tavily-specific SearchWeb implementation with SearXNG.
- Does not require a commercial search API key.
- Has no paid fallback and makes no promise of unlimited availability; upstream engines and public instances can rate-limit searches.

1) ADD THE TOOL
- Copy Tools/WebTools.cs into your MCP server's Tools folder.
- Remove or replace the old Brave-specific WebTools.cs so there is only one WebTools class/tool named SearchWeb.
- Merge Program.cs-searxng-snippet.txt into your existing Program.cs.
- Add .WithTools<WebTools>() to the EXISTING AddMcpServer() chain alongside ContentTools, MediaTools, and HomeTools.

2) CONFIGURE THE INSTANCE
Add an appsettings.Development.json setting for local development (do not commit a private endpoint if you don't want it public):
{
  "SearXNG": {
    "BaseUrl": "http://localhost:8080/"
  }
}

For a public SearXNG instance, set BaseUrl to its HTTPS base URL, ending in /. Public instances are operated by third parties, may log queries, may disable JSON output, and may throttle/block automated traffic. Pick one you trust and respect its rules. Do not repeatedly probe public instances.

For a self-hosted instance, enable JSON output in its settings.yml:
search:
  formats:
    - html
    - json

The SearXNG API expects /search?q=...&format=json. This tool sends one request per search and returns a maximum of 10 results.

3) LOCAL SELF-HOSTING EXAMPLE
If Docker is installed, you can run SearXNG locally for development, but a default setup may need configuration to enable JSON output. Follow the official instructions:
https://docs.searxng.org/admin/installation-searxng.html
Search API docs:
https://docs.searxng.org/dev/search_api.html

4) IMPORTANT FOR AZURE
If your MCP server runs in Azure, it cannot reach http://localhost:8080 on your personal PC. The configured SearXNG instance must be reachable from Azure. A public instance is not guaranteed unlimited or reliable; hosting your own instance on an always-on publicly reachable machine may incur infrastructure costs.

5) TEST
- Build the project.
- Restart the MCP server.
- Refresh the MCP tool list in Postman.
- Call SearchWeb with a simple query.
- If it returns HTTP 403 or says JSON is disabled, use an instance whose operator explicitly enables JSON output or self-host/configure your own.

SECURITY / LIMITATIONS
- Treat returned titles/snippets as untrusted external content.
- This tool does not fetch the pages themselves; webpage retrieval should be a separate tool with SSRF protections.
- This starter does not bypass rate limits, CAPTCHAs, or access controls.

