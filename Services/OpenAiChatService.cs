using System.Text.Json;
using System.Text;

namespace JustAskIndia.Services
{
    public class OpenAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public OpenAiChatService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> AskChatAsync(string systemPrompt, string userPrompt)
        {
            var apiKey = _config["OpenAI:ApiKey"];

            var request = new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(req);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
    }

}
