using JustAskIndia.Models;


namespace JustAskIndia.DTOs;

public class UserDTO
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}

public class UserAclDTO
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
}

public class CachedUserDTO
{
    public int Id { get; set; }
    public AppUser? AppUser { get; set; }
    public string? Role { get; set; }
}

public class SearchResult
{
    public string Message { get; set; } = string.Empty; 
    public List<Business> Results { get; set; } = new();
}