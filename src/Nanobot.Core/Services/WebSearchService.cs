namespace Nanobot.Core.Services;

using System.Text.Json;
using Nanobot.Core.Models;

public interface IWebSearchService
{
    Task<List<SearchResult>> SearchAsync(string query, int count = 10, CancellationToken cancellationToken = default);
}

public record SearchResult
{
    public string Title { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Snippet { get; init; } = string.Empty;
}

public class DuckDuckGoSearchService : IWebSearchService
{
    private readonly HttpClient _httpClient;

    public DuckDuckGoSearchService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; nanobot/0.1.5)");
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            // Using DuckDuckGo instant answer API (simplified - no API key needed)
            var searchUrl = $"https://api.duckduckgo.com/?q={Uri.EscapeDataString(query)}&format=json&no_html=1";
            var response = await _httpClient.GetStringAsync(searchUrl, cancellationToken);
            
            var results = new List<SearchResult>();
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            // Get abstract text if available
            if (root.TryGetProperty("AbstractText", out var abstractText) && 
                !string.IsNullOrEmpty(abstractText.GetString()))
            {
                results.Add(new SearchResult
                {
                    Title = root.TryGetProperty("Heading", out var heading) ? heading.GetString() ?? "" : "",
                    Url = root.TryGetProperty("AbstractURL", out var url) ? url.GetString() ?? "" : "",
                    Snippet = abstractText.GetString() ?? ""
                });
            }

            // Get related topics
            if (root.TryGetProperty("RelatedTopics", out var relatedTopics))
            {
                foreach (var topic in relatedTopics.EnumerateArray().Take(count - results.Count))
                {
                    var result = new SearchResult();
                    
                    if (topic.TryGetProperty("Text", out var text))
                    {
                        result = result with { Snippet = text.GetString() ?? "" };
                    }
                    
                    if (topic.TryGetProperty("FirstURL", out var firstUrlProp))
                    {
                        result = result with { Url = firstUrlProp.GetString() ?? "" };
                    }
                    
                    results.Add(result);
                }
            }

            return results.Take(count).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Web search error: {ex.Message}");
            return new List<SearchResult>();
        }
    }
}
