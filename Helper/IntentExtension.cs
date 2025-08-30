using System.Text;
using JustAskIndia.DTOs;

namespace JustAskIndia.Helper
{
    public static class IntentExtensions
    {
        public static string GenerateVectorContent(this QueryIntent intent)
        {
            StringBuilder sb = new StringBuilder();
            if(!string.IsNullOrEmpty(intent.Name))
                sb.AppendLine($"Name: {intent.Name}");

            if(!string.IsNullOrEmpty(intent.Address))
                sb.AppendLine($"Address: {intent.Address}");

            if (!string.IsNullOrEmpty(intent.City))
                sb.AppendLine($"City: {intent.City}");

            if (!string.IsNullOrEmpty(intent.State))
                sb.AppendLine($"State: {intent.State}");

            if (!string.IsNullOrEmpty(intent.Pin))
                sb.AppendLine($"Pin: {intent.Pin}");

            if (!string.IsNullOrEmpty(intent.PhoneNumber))
                sb.AppendLine($"Phone Number: {intent.PhoneNumber}");

            if (!string.IsNullOrEmpty(intent.MobileNumber))
                sb.AppendLine($"Mobile Number: {intent.MobileNumber}");

            if (!string.IsNullOrEmpty(intent.Email))
                sb.AppendLine($"Email: {intent.Email}");

            if (!string.IsNullOrEmpty(intent.ProductsServices))
                sb.AppendLine($"Product/Services: {intent.ProductsServices}");

            if (!string.IsNullOrEmpty(intent.Website))
                sb.AppendLine($"Website: {intent.Website}");


            return sb.ToString();
        }
    }

}
