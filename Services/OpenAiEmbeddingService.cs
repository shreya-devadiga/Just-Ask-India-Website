using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JustAskIndia.Models;
using Microsoft.Extensions.Configuration;

namespace JustAskIndia.Services
{
    public class OpenAiEmbeddingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public OpenAiEmbeddingService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI API Key not found");
        }

        public async Task<float[]?> GetEmbeddingAsync(string input)
        {
            var requestData = new
            {
                input = input,
                model = "text-embedding-3-small"
            };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.openai.com/v1/embeddings"),
                Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var embeddingArray = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            return embeddingArray;
        }

        public async Task<string> GetChatResponseAsync(string prompt)
        {
            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant that extracts structured data from user queries." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.openai.com/v1/chat/completions"),
                Content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?
                .Trim() ?? "";
        }

        public async Task<List<int>> ValidateSearchResultsAsync(
             string userQuery,
             List<string> resultSummaries
         )
        {
            var endpoint = "https://api.openai.com/v1/chat/completions";

            var numberedResults = string.Join("\n", resultSummaries.Select((r, i) => $"{i + 1}. {r}"));

            var prompt = $"""
You are a smart assistant helping users find the most relevant company based on their query.

Original User Query: "{userQuery}"

List of Company Descriptions (numbered):
{numberedResults}
Each record contains:
- Company Name
- Address
- City
- State
- Products/Services

Instructions:
- If the user query contains a location, return ONLY results that clearly mention that location (or an exact synonym) in the City, Address, or State fields.
- Do NOT include results from other cities, even if they are in the same district or nearby.
- If the query mentions a product/service (e.g., "turmeric"), only return companies that clearly provide that specific item. No partial or broad matches.
- Prefer results that match **both product/service AND location**.
- Do NOT guess or assume — only return results that fully match the complete intent of the user query.
- If the user query contains a location, match results where that location name (or a very close variant) appears in the **City**, **Address**, or **State** fields — matches are valid even if the location appears with words like "Taluka", "Village", etc.
- If the query contains a product/service, match only companies that clearly offer that item (case-insensitive exact match within Products/Services).
- Do not guess or include partial/broad matches that don't clearly meet all requirements.
- Thoroughly search the results and only return matches that are very similar or exact to the user's query intent.
- If no good match is found, return an empty array.
Response format:
Return a plain JSON array of 1-based indexes. Examples:
- Matches found: [1, 3]
- No matches: []
""";
        
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
            new { role = "system", content = "You are an expert AI assistant who only returns exact and similar matches. No guessing." },
            new { role = "user", content = prompt }
        },
                temperature = 0.3
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(endpoint, jsonContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                var json = JsonDocument.Parse(responseContent);
                var messageContent = json.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                var matchArray = ExtractJsonArray(messageContent);

                var indexList = new List<int>();
                foreach (var item in matchArray.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number)
                    {
                        indexList.Add(item.GetInt32() - 1); // Convert to 0-based index
                    }
                }

                return indexList;
            }
            catch
            {
                return new List<int>();
            }
        }

        private JsonElement ExtractJsonArray(string content)
        {
            var match = Regex.Match(content, @"\[(.*?)\]", RegexOptions.Singleline);
            if (!match.Success)
                throw new Exception("No valid JSON array found in response.");

            var jsonArray = "[" + match.Groups[1].Value + "]";

            var doc = JsonDocument.Parse(jsonArray);
            return doc.RootElement;
        }

        public async Task<string> GenerateFormattedSummaryAsync(string prompt)
        {
            var requestBody = new
            {
                model = "gpt-4",  
                messages = new[]
                {
            new { role = "system", content = "You are a helpful assistant." },
            new { role = "user", content = prompt }
        },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient(); 
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }


    }
}
