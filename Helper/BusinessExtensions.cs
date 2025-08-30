using JustAskIndia.Models;

namespace JustAskIndia.Helpers
{
    public static class BusinessExtensions
    {
        public static string GenerateVectorContent(this Business b)
        {
          return$"Name: {b.Name}\nAddress: {b.Address}\nCity: {b.City}\nPin: {b.Pin}\nState: {b.State}\nPhone Number: {b.PhoneNumber}\nMobile Number: {b.MobileNumber} Email: {b.Email}\nProduct/Services: {b.ProductsServices}\nWebsite: {b.Website}";
        }
    }
}
