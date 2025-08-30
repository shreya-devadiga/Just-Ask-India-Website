using JustAskIndia.DTOs;

namespace JustAskIndia.Services
{
    public interface IGeocodingService
    {
        Task<AddressGeocodeDto?> GetCoordinatesAsync(string? address);
        Task<IpGeoLocationDto?> GetLatLongFromIPAsync(string ipAddress);
    }
}