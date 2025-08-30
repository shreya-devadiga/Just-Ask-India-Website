using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using JustAskIndia.Services;

namespace JustAskIndia.Controllers
{
    [ApiController]
    [Route("api/interakt/webhook")]
    public class InteraktWebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly BusinessSearchService _searchService;

        public InteraktWebhookController(IConfiguration config, BusinessSearchService searchService)
        {
            _config = config;
            _searchService = searchService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement body)
        {
            //  Verify Interakt Signature
            if (!Request.Headers.TryGetValue("x-hub-signature-256", out var signatureHeader))
                return Unauthorized();

            string webhookSecret = _config["Interakt:WebhookSecret"];
            string bodyRaw = body.GetRawText();

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyRaw));
            string expectedSignature = "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToLower();

            if (expectedSignature != signatureHeader)
                return Unauthorized();

            // Extract message
            var userMessage = body.GetProperty("event").GetProperty("payload").GetProperty("text").GetString();
            var fromPhone = body.GetProperty("event").GetProperty("payload").GetProperty("from").GetString();

            if (string.IsNullOrEmpty(userMessage))
                return Ok();

            // Call your BusinessSearchService
            var (results, suggestion, readableResults) = await _searchService.SearchByPromptAsync(
                userPrompt: userMessage,
                similarityThreshold: 1.2f,
                username: fromPhone,
                isConfirmed: false,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            );

            string reply = string.IsNullOrWhiteSpace(readableResults)
                ? "❌ Sorry, no businesses found."
                : readableResults;

            // Send reply back via Interakt API
            await SendMessageAsync(fromPhone, reply);

            return Ok();
        }

        private async Task SendMessageAsync(string to, string message)
        {
            string apiKey = _config["Interakt:ApiKey"];
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var payload = new
            {
                phoneNumber = to,
                type = "text",
                text = new { body = message }
            };

            var response = await client.PostAsJsonAsync(
                "https://api.interakt.ai/v1/public/message/",
                payload
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Interakt Send Error: " + error);
            }
        }
    }
}
