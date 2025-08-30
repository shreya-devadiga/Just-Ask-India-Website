using JustAskIndia.Models;
using JustAskIndia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JustAskIndia.Pages
{
    public class IndexModel : PageModel
    {
        private readonly BusinessSearchService _searchService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GeocodingService _geocodingService;

        public IndexModel(
            BusinessSearchService searchService,
            IHttpContextAccessor httpContextAccessor,
            GeocodingService geocodingService)
        {
            _searchService = searchService;
            _httpContextAccessor = httpContextAccessor;
            _geocodingService = geocodingService;
        }

        [BindProperty]
        public string UserPrompt { get; set; } = "";

        [BindProperty]
        public double Latitude { get; set; }

        [BindProperty]
        public double Longitude { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SuggestedPrompt { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsConfirmed { get; set; }

        public string ReadableResults { get; set; } = string.Empty;

        public List<Business> Results { get; set; } = new();

        public List<(Business Business, double DistanceKm)> NearbyBusinessesWithDistance { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            const float similarityThreshold = 1.2f;
            var username = User.Identity?.Name ?? "guest";
            string ipAddress = Utilities.Constants.IsProduction ? GetRequestIP() ?? "Unknown IP" : "14.142.182.247";

            double userLat, userLng;

            // Get user location: from form or fallback to IP geocoding
            if (Latitude == 0 && Longitude == 0)
            {
                (userLat, userLng) = await _geocodingService.GetLatLongFromIPAsync(ipAddress);
            }
            else
            {
                userLat = Latitude;
                userLng = Longitude;
            }

            if (string.IsNullOrWhiteSpace(UserPrompt))
                return Page();

            UserPrompt = UserPrompt.Trim();

            // Perform semantic vector search
            var (searchResults, suggestion, readableResults) = await _searchService.SearchByPromptAsync(
                userPrompt: UserPrompt,
                similarityThreshold: similarityThreshold,
                username: username,
                isConfirmed: false,
                ipAddress: ipAddress
            );

            // If no results and suggestion exists, keep it
            if (searchResults.Count == 0 && !string.IsNullOrWhiteSpace(suggestion))
            {
                if (string.Equals(UserPrompt, suggestion, StringComparison.OrdinalIgnoreCase))
                    suggestion = string.Empty;
            }

            SuggestedPrompt = suggestion;
            IsConfirmed = false;

            bool isNearMeRequest = Request.Form["nearMe"] == "true";

            if (isNearMeRequest && searchResults.Any())
            {
                
                var businessIds = searchResults.Select(b => b.Id).ToList();

                var (nearbyList, nearbyReadable) = await _searchService.FilterNearbyBusinessesAsync(
                    businessIds: businessIds,
                    userLat: userLat,
                    userLng: userLng,
                    userPrompt: UserPrompt
                );

                NearbyBusinessesWithDistance = nearbyList;
                Results.Clear();
                ReadableResults = nearbyReadable; 
            }
            else
            {
                
                Results = searchResults;
                NearbyBusinessesWithDistance.Clear();
                ReadableResults = readableResults;
            }

            return Page();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!string.IsNullOrWhiteSpace(SuggestedPrompt) && IsConfirmed)
            {
                const float similarityThreshold = 1.2f;
                var username = User.Identity?.Name ?? "guest";
                string ipAddress = Utilities.Constants.IsProduction ? GetRequestIP() ?? "Unknown IP" : "14.142.182.247";

                var (results, _, readableResults) = await _searchService.SearchByPromptAsync(
                    userPrompt: SuggestedPrompt,
                    similarityThreshold: similarityThreshold,
                    username: username,
                    isConfirmed: true,
                    ipAddress: ipAddress
                );

                Results = results;
                UserPrompt = SuggestedPrompt;
                SuggestedPrompt = null;
                ReadableResults = readableResults;
            }

            return Page();
        }

        private string? GetRequestIP(bool tryUseXForwardHeader = true)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string? ip = null;

            if (tryUseXForwardHeader)
            {
                ip = GetHeaderValueAs<string>("X-Forwarded-For")?.Split(',').FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(ip) && httpContext?.Connection?.RemoteIpAddress != null)
            {
                ip = httpContext.Connection.RemoteIpAddress.ToString();
            }

            if (string.IsNullOrWhiteSpace(ip))
            {
                ip = GetHeaderValueAs<string>("REMOTE_ADDR");
            }

            return string.IsNullOrWhiteSpace(ip) ? null : ip;
        }

        private T? GetHeaderValueAs<T>(string headerName)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.Request?.Headers?.TryGetValue(headerName, out StringValues values) ?? false)
            {
                var rawValues = values.ToString();
                if (!string.IsNullOrWhiteSpace(rawValues))
                    return (T)Convert.ChangeType(rawValues, typeof(T));
            }

            return default;
        }
    }
}
