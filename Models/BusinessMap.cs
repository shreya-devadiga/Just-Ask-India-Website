using CsvHelper.Configuration;


namespace JustAskIndia.Models
{
    public class BusinessMap : ClassMap<Business>
    {
        public BusinessMap()
        {
            Map(m => m.Name).Name("COMPANY NAME");
            Map(m => m.Address).Name("ADD");
            Map(m => m.City).Name("CITY");
            Map(m => m.Pin).Name("PIN");
            Map(m => m.State).Name("STATE");
            Map(m => m.PhoneNumber).Name("PHONE NO.");
            Map(m => m.MobileNumber).Name("MOBILE NO.");
            Map(m => m.Email).Name("EMAIL");
            Map(m => m.Website).Name("WEB");
            Map(m => m.ProductsServices).Name("DETAILS");
        }
    }
}