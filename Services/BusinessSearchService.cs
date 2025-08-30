using JustAskIndia.Models;
using JustAskIndia.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Pgvector;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;
using JustAskIndia.Helpers;
using System.Text;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;
using System.Xml.Linq;
using JustAskIndia.Helper;
using Microsoft.VisualBasic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;

namespace JustAskIndia.Services
{
    public class BusinessSearchService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly OpenAiEmbeddingService _embeddingService;
        private readonly QueryCorrectionService _correctionService;
        private readonly OpenAiIntentExtractor _intentExtractor;

        public BusinessSearchService(
            AppDbContext context,
            IConfiguration configuration,
            OpenAiEmbeddingService embeddingService,
            QueryCorrectionService correctionService,
            OpenAiIntentExtractor intentExtractor)
        {
            _context = context;
            _configuration = configuration;
            _embeddingService = embeddingService;
            _correctionService = correctionService;
            _intentExtractor = intentExtractor;
        }

        public async Task<(List<Business> Results, string Suggestion, string ReadableResults)> SearchByPromptAsync(
    string userPrompt, float similarityThreshold, string username, bool isConfirmed, string ipAddress)
        {
            var results = new List<Business>();
            string finalSuggestion = string.Empty;
            string readableResults = string.Empty;

            if (string.IsNullOrWhiteSpace(userPrompt))
                return (results, finalSuggestion, readableResults);

            // Extract intent from user query
            var intent = await _intentExtractor.ExtractIntentAsync(userPrompt);
            if (intent == null)
                return (results, finalSuggestion, readableResults);

            //string vectorQuery = userPrompt;
            string vectorQuery = intent.GenerateVectorContent();

            if (string.IsNullOrWhiteSpace(vectorQuery))
                return (results, finalSuggestion, readableResults);

            // Generate vector from intent
            var embeddingArray = await _embeddingService.GetEmbeddingAsync(vectorQuery);
            if (embeddingArray == null || embeddingArray.Length != 1536)
                return (results, finalSuggestion, readableResults);


            var searchVector = new Vector(embeddingArray);

            // Database search using pgvector
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_configuration.GetConnectionString("DefaultConnection"));
            dataSourceBuilder.UseVector();
            await using var dataSource = dataSourceBuilder.Build();
            await using var connection = await dataSource.OpenConnectionAsync();

            try
            {
                const string sql = @"
                 SELECT *, (""Embedding"" <-> @embedding) AS similarity
                    FROM ""Businesses""
                    WHERE ""Embedding"" IS NOT NULL
                    ORDER BY similarity ASC
                    LIMIT 50;
                    ";

                await using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("embedding", searchVector);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    results.Add(MapBusiness(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Embedding DB Search Error: " + ex.Message);
            }


            if (results.Count > 0)
            {
                var descriptions = results.Select(b => b.VectorContent).ToList();
                var validIndexes = await _embeddingService.ValidateSearchResultsAsync(userPrompt, descriptions);

                if (validIndexes.Any())
                {
                    var result = await GenerateReadableResultsAsync(userPrompt, descriptions, validIndexes);
                    readableResults= Markdig.Markdown.ToHtml(result);   
                    results = validIndexes.Select(index => results[index]).ToList();
                }
                else
                {
                    readableResults = $"❌ Nothing was found : \"{userPrompt}\".";
                    results.Clear();
                }
            }

            if (!results.Any() && !isConfirmed)
                {
                    var (spellingMistakes, searchSuggestions) = await _correctionService.SuggestCorrectionAsync(
                        userPrompt,
                        results.Select(n => n.VectorContent).ToList()
                    );

                    if (searchSuggestions.Any() &&
                        !searchSuggestions.Contains(userPrompt, StringComparer.OrdinalIgnoreCase))
                    {
                        finalSuggestion = searchSuggestions.First();
                    }
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (user != null)
                {
                    _context.AppUserLogins.Add(new UserLogin
                    {
                        AppUserId = user.Id,
                        Login = DateTime.UtcNow,
                        Prompt = userPrompt,
                        IpAddress = ipAddress
                    });
                    await _context.SaveChangesAsync();
                }

            return (results, finalSuggestion, readableResults);
        }
        public async Task<string> GenerateReadableResultsAsync(
            string userQuery,
            List<string> resultSummaries,
            List<int> validatedIndexes)
        {
            if (validatedIndexes == null || validatedIndexes.Count == 0)
            {
                return $"❌ No companies were found that exactly match your query: \"{userQuery}\".";
            }

            var matchedSummaries = validatedIndexes
                .Where(i => i >= 0 && i < resultSummaries.Count)
                .Select((i, idx) => $"Match #{idx + 1}:\n{resultSummaries[i]}")
                .ToList();

            var combinedSummary = string.Join("\n\n", matchedSummaries);

            var prompt = $@"
You are a helpful assistant.

The user searched for: **\""{userQuery}\""**.

Instructions:
1.If the query is for a **specific detail**:
   - Respond ONLY with that detail in one short, direct sentence.
     Example: 'The address of [Business Name] is [Address].'
   - If the detail is **missing**, say: 'The [detail] of [Business Name] is not available, but you can contact them at [Phone] or [Email].'
   - Do NOT include any other details unless the requested detail is missing.

2. If the query is for general detail:
   - If ANY of the details (email, website, phone, pin, address) is missing for a business, write a one-by-one **ChatGPT-style summary** for each business.
   - If all details are present, still provide a compact, neat summary.

3.If the query has multiple results then list them as bullet points. Don't start with 'Match #'.

4. Formatting for summaries:
   - Bold the **keywords** from the user query.
   - Bold important details like **Address**, **City**, **State**, **Pin**, **Products/Services**, **Phone**, **Email**, **Website**.
   - For websites, use clickable HTML links like `<a href=\""http://example.com\"" target=\""_blank\"">example.com</a>`.

Business Matches:
{combinedSummary}
";

            var formattedOutput = await _embeddingService.GenerateFormattedSummaryAsync(prompt);
            return formattedOutput;
        }



        // Haversine location filtering
        public async Task<(List<(Business Business, double DistanceKm)> Results, string ReadableResults)>
    FilterNearbyBusinessesAsync(
        List<int> businessIds, double userLat, double userLng, string userPrompt, double maxDistanceKm = 50)
        {
            var results = new List<(Business Business, double DistanceKm)>();
            string readableResults = string.Empty;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_configuration.GetConnectionString("DefaultConnection"));
            await using var dataSource = dataSourceBuilder.Build();
            await using var connection = await dataSource.OpenConnectionAsync();

            const string sql = @"
WITH CalculatedDistances AS (
    SELECT *,
        6371 * acos(
            cos(radians(@userLat)) * cos(radians(""Latitude"")) *
            cos(radians(""Longitude"") - radians(@userLng)) +
            sin(radians(@userLat)) * sin(radians(""Latitude""))
        ) AS distance_km
    FROM ""Businesses""
    WHERE 
        ""Id"" = ANY(@ids) AND
        ""Latitude"" IS NOT NULL AND
        ""Longitude"" IS NOT NULL
)
SELECT *, distance_km
FROM CalculatedDistances
WHERE distance_km <= @maxDistance
ORDER BY distance_km ASC;
";

            await using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("userLat", userLat);
            cmd.Parameters.AddWithValue("userLng", userLng);
            cmd.Parameters.AddWithValue("maxDistance", maxDistanceKm);
            cmd.Parameters.AddWithValue("ids", businessIds.ToArray());

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var business = MapBusiness(reader);
                double distance = reader.GetDouble(reader.GetOrdinal("distance_km"));
                results.Add((business, distance));
            }

            if (results.Any())
            {
                var descriptions = results
                     .Select(r => $"Nearby businesses are:{r.Business.VectorContent}\n**Distance:** **{r.DistanceKm:F1}** km")
                     .ToList();
                var validIndexes = Enumerable.Range(0, results.Count).ToList();
                var resultText = await GenerateReadableResultsAsync(userPrompt, descriptions, validIndexes);
                readableResults = Markdig.Markdown.ToHtml(resultText);
            }

            return (results, readableResults);
        }




        // Maps business from Npgsql reader
        private Business MapBusiness(NpgsqlDataReader reader)
        {
            return new Business
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.SafeGetString("Name"),
                Address = reader.SafeGetString("Address"),
                City = reader.SafeGetString("City"),
                Pin = reader.SafeGetString("Pin"),
                State = reader.SafeGetString("State"),
                PhoneNumber = reader.SafeGetString("PhoneNumber"),
                MobileNumber = reader.SafeGetString("MobileNumber"),
                Email = reader.SafeGetString("Email"),
                Website = reader.SafeGetString("Website"),
                ProductsServices = reader.SafeGetString("ProductsServices"),
                VectorContent = reader.SafeGetString("VectorContent"),
                Latitude = reader.SafeGetNullableDouble("Latitude"),
                Longitude = reader.SafeGetNullableDouble("Longitude")
            };
        }
    }
   

    // Safe access helpers
    public static class NpgsqlExtensions
    {
        public static string SafeGetString(this NpgsqlDataReader reader, string column)
        {
            int index = reader.GetOrdinal(column);
            return reader.IsDBNull(index) ? "" : reader.GetString(index);
        }

        public static double? SafeGetNullableDouble(this NpgsqlDataReader reader, string column)
        {
            int index = reader.GetOrdinal(column);
            return reader.IsDBNull(index) ? (double?)null : reader.GetDouble(index);
        }
    }
}