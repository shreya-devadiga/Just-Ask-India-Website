using NpgsqlTypes;
using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace JustAskIndia.Models
{
    public class Business
    {
        public int Id { get; set; }

        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Pin { get; set; }
        public string? State { get; set; }
        public string? PhoneNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? ProductsServices { get; set; }

        public string? VectorContent { get; set; }

        [Column(TypeName = "vector(1536)")]
        public Vector? Embedding { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
