namespace JustAskIndia.DTOs
{
    public class AddressGeocodeDto
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public void Deconstruct(out double? latitude, out double? longitude)
        {
            latitude = Latitude;
            longitude = Longitude;
        }
    }
    
}
