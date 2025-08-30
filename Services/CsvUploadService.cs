using CsvHelper;
using CsvHelper.Configuration;
using JustAskIndia.Data;
using JustAskIndia.Helpers;
using JustAskIndia.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using System.Globalization;

namespace JustAskIndia.Services
{
    public class CsvUploadService
    {
        private readonly AppDbContext _context;
        private readonly OpenAiEmbeddingService _embeddingService;
        private readonly GeocodingService _geocodingService;

        public CsvUploadService(
            AppDbContext context,
            OpenAiEmbeddingService embeddingService,
            GeocodingService geocodingService)
        {
            _context = context;
            _embeddingService = embeddingService;
            _geocodingService = geocodingService;
        }

        public async Task UploadAndSaveAsync(Stream fileStream)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant(),
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true
            };

            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, config);

            try
            {
                csv.Context.RegisterClassMap<BusinessMap>();
                var records = csv.GetRecords<Business>().ToList();

                foreach (var record in records)
                {
                    if (record == null)
                    {
                        Console.WriteLine("⚠️ Null record encountered. Skipping.");
                        continue;
                    }

                    // Sanitize fields
                    record.Name = record.Name?.Trim();
                    record.City = record.City?.Trim();
                    record.Address = record.Address?.Trim();
                    record.Pin = record.Pin?.Trim();
                    record.State = record.State?.Trim();

                    // Generate vector content
                    var vectorContent = record.GenerateVectorContent()?.Trim();
                    if (string.IsNullOrWhiteSpace(vectorContent))
                    {
                        Console.WriteLine($"⚠️ Skipping: Empty vector content for '{record.Name ?? "Unknown"}'");
                        continue;
                    }

                    record.VectorContent = vectorContent;

                    // Get embedding
                    var embeddingArray = await _embeddingService.GetEmbeddingAsync(vectorContent);
                    if (embeddingArray == null || embeddingArray.Length != 1536)
                    {
                        Console.WriteLine($"❌ Embedding failed for '{record.Name ?? "Unknown"}'");
                        continue;
                    }

                    record.Embedding = new Vector(embeddingArray);

                    // Get coordinates
                    var fullAddress = $"{record.Address}, {record.City}, {record.State}, {record.Pin}";
                    var coords = await _geocodingService.GetCoordinatesAsync(fullAddress);

                    if (coords == null)
                    {
                        Console.WriteLine($"⚠️ Coordinates not found for '{record.Name ?? "Unknown"}'");
                        continue;
                    }

                    record.Latitude = coords.Latitude;
                    record.Longitude = coords.Longitude;

                    // Check for duplicates
                    var nameToCheck = record.Name?.ToLowerInvariant();
                    var cityToCheck = record.City?.ToLowerInvariant();

                    bool exists = await _context.Businesses.AnyAsync(b =>
                        (b.Name ?? "").Trim().ToLower() == nameToCheck &&
                        (b.City ?? "").Trim().ToLower() == cityToCheck);

                    if (!exists)
                    {
                        _context.Businesses.Add(record);
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Duplicate business found: {record.Name}. Skipping insert.");
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("✅ CSV upload complete. All valid businesses saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception during CSV processing: {ex.Message}");
                throw;
            }
        }
    }
}
