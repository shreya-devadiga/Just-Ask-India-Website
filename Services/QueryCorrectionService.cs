using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace JustAskIndia.Services
{
    public class QueryCorrectionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public QueryCorrectionService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        public async Task<(List<string> SpellingMistakes, List<string> SearchQueries)> SuggestCorrectionAsync(string userQuery, List<string> items)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var endpoint = "https://api.openai.com/v1/chat/completions";

            var systemPrompt = @"
You are an intelligent assistant helping users find business-related services or products from a database.

The user’s query did not return any relevant business results.
You must do the following:
1. Detect and correct any spelling or grammatical mistakes in the query.
2. Correct any **place names (like cities, localities)** based on what is found in the database search result list.
3. DO NOT suggest anything not found in the search result list.
4. Map the user’s intent only to available entries in the database.
5. Identify spelling or irrelevant words (like 'manufacturer', 'store', etc.) that make the query not match database entries.
6. Suggest corrected queries **only if the corrected term exists exactly in the available search results below** or database.
7. Return the output in this exact format:

- Spelling Mistakes:
    [""correction1"", ""correction2""]
- Search query:
    [""query1"", ""query2""]

8. Ensure that the search queries include the **entire corrected version** of the user’s original query (not just the corrected business name). For example:
   User: what is number of brwn leaf in karnataka  
   Output: what is number of brown leaf in karnataka

   User: what is number of brwn leaf 
   Output: what is number of brown leaf 

   User: Termex Inda
   Output: THERMEX INDIA

9. If the corrected query (after spelling or location fixes) **still does not return results**, then **do not show any suggestion at all**.
";

            // Extract readable names from vector JSON (like "Thermex India", etc.)
            var businessNames = items
                .Select(json =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var name = doc.RootElement.GetProperty("name").GetString();
                        var location = doc.RootElement.TryGetProperty("location", out var locProp)
                            ? locProp.GetString()
                            : "";
                        return $"{name} {(string.IsNullOrWhiteSpace(location) ? "" : location)}".Trim();
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            var searchResults = string.Join("\n", businessNames.Select(i => $"- {i}"));

            var userPrompt = $@"User Query: {userQuery}

Search Results from database:
{searchResults}";

            var payload = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.2
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (new List<string>(), new List<string>());

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Use helper to parse GPT output
            TextExtractor.ExtractSpellingAndQuery(content!, out var spellingMistakes, out var searchQueries);
            return (spellingMistakes, searchQueries);
        }
    }
}
