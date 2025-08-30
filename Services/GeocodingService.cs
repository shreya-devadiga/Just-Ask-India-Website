// File: Services/GeocodingService.cs
using System.Net.Http;
using System.Text.Json;
using JustAskIndia.DTOs;
using JustAskIndia.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace JustAskIndia.Services
{
    public class GeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeocodingService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GoogleMaps:Key"] ?? throw new Exception("Google API key not configured");
        }

        public async Task<AddressGeocodeDto?> GetCoordinatesAsync(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "OK")
                return null;

            var location = root.GetProperty("results")[0]
                               .GetProperty("geometry")
                               .GetProperty("location");

            return new AddressGeocodeDto
            {
                Latitude = location.GetProperty("lat").GetDouble(),
                Longitude = location.GetProperty("lng").GetDouble()
            };
        }

        public async Task<IpGeoLocationDto?> GetLatLongFromIPAsync(string ipAddress)
        {
            var url = $"https://ipwho.is/{ipAddress}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<IpGeoLocationDto>(json, options);

            return result?.Success == true ? result : null;
        }
    }
}
