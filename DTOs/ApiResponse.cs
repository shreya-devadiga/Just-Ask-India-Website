namespace JustAskIndia.DTOs
{
    public class ApiResponse
    {
        public List<EmbeddingData> Data { get; set; } = new();
    }

    public class EmbeddingData
    {
        public List<float> Embedding { get; set; } = new();
    }
}