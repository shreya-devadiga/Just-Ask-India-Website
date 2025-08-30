using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using JustAskIndia.DTOs;

public class OpenAiIntentExtractor
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiIntentExtractor(IHttpClientFactory factory, IConfiguration config)
    {
        _httpClient = factory.CreateClient();
        _apiKey = config["OpenAI:ApiKey"];
    }

    private static readonly string systemPrompt = @"
You are a query understanding engine that extracts structured details from user queries.

Extract the following fields:
- ProductsServices: Goods or services being searched (e.g., turmeric, CCTV installation)
- Name: Business or brand name (e.g., Diamond Plywood, Finolex Cables)
- Address: Any location detail including address,street or area mentioned (e.g., '123 Main St', 'Koramangala')
- City: City name if clearly mentioned
- State: State name if available (e.g., Karnataka)
- Pin: Pincode if present (e.g., 560001) – this is also called zip code
- Phone Number: Include if landline, phone number, mobile number, or contact is mentioned
- Mobile Number: Include if mobile number, phone number, or contact is mentioned
- Email: If email address is mentioned
- Website: If website or web is asked or mentioned

Rules:
- If a location includes smaller administrative regions (village, taluka, tehsil, district), store them in the Address field, and do not drop them if no city is found.
- Treat 'taluka', 'district', 'village', and 'tehsil' as valid location parts — do NOT convert or remove them.
- If a term could be a brand or a product, include it in both ProductsServices and Name.
- Treat 'phone number', 'mobile number', 'landline', and 'contact' as interchangeable.
- If any one of these (phone, mobile, landline, contact) is requested, return all numbers available in both PhoneNumber and MobileNumber fields.
- Always populate both PhoneNumber and MobileNumber fields with the same available numbers if only one is requested.
- Try to fill as many fields as possible.
- Return null for any field not found.
- Only output JSON in the below format.
- Never return ""null"" as a string. Use actual null.

Respond ONLY with JSON in this format:
{
  ""ProductsServices"": ""..."",
  ""Name"": ""..."",
  ""Address"": ""..."",
  ""City"": ""..."",
  ""State"": ""..."",
  ""Pin"": ""..."",
  ""Phone Number"": ""..."",
  ""Mobile Number"": ""..."",
  ""Email"": ""..."",
  ""Website"": ""...""
}
";

    public async Task<QueryIntent?> ExtractIntentAsync(string userQuery)
    {
        var requestBody = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userQuery }
            },
            temperature = 0.2
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        try
        {
            var json = JsonDocument.Parse(responseBody);
            var resultString = json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?
                .Trim()
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            Console.WriteLine("CLEANED OpenAI Response:\n" + resultString);

            if (!string.IsNullOrWhiteSpace(resultString))
            {
                var parsed = JsonSerializer.Deserialize<QueryIntent>(resultString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if(parsed is not null)
                {
                    if(string.IsNullOrEmpty(parsed.Address) 
                        && (!string.IsNullOrEmpty(parsed.City) || !string.IsNullOrEmpty(parsed.State) || !string.IsNullOrEmpty(parsed.Pin)))
                    {
                        parsed.Address = "";
                        parsed.Address += !string.IsNullOrEmpty(parsed.City) ? parsed.City + ", " : "";
                        parsed.Address += !string.IsNullOrEmpty(parsed.State) ? parsed.State + ", " : "";
                        parsed.Address += !string.IsNullOrEmpty(parsed.Pin) ? parsed.Pin : "";
                    }
                }                

               
                return parsed;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error extracting intent: " + ex.Message);
            Console.WriteLine("Raw Response:\n" + responseBody);
        }

        return null;
    }
}
