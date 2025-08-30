namespace JustAskIndia.DTOs
{
    public class QueryIntent
    {
        public string? Category { get; set; }
        public string? ProductsServices { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pin { get; set; }
        public string? PhoneNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? CorrectedQuery { get; set; }
    }
    public class IntentResult
    {
        public QueryIntent? QueryIntent { get; set; }
        public bool IsCorrected { get; set; }
        public string? CorrectedQuery { get; set; }
    }
}
