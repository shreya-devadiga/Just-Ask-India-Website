namespace JustAskIndia.DTOs
{
    public class IpGeoLocationDto
    {
        public bool Success { get; set; }
        public string Ip { get; set; } = "";
        public string Country { get; set; } = "";
        public string Region { get; set; } = "";
        public string City { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public void Deconstruct(out double latitude, out double longitude)
        {
            latitude = Latitude;
            longitude = Longitude;
        }
    }
}
