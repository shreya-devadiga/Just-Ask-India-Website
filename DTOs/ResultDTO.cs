namespace JustAskIndia.DTOs;

public class ResultDTO<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public ResultDTO()
    {
        Success = false;
        Message = "There was and error while processing your request. Try again.";
    }

    public ResultDTO(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public ResultDTO(bool success, string message, T data)
    {
        Success = success;
        Message = message;
        Data = data;
    }
}
