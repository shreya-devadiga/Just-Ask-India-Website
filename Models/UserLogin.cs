using JustAskIndia.Models;

namespace JustAskIndia.Models;

public class UserLogin
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public DateTime? Login { get; set; }

    public AppUser? AppUser { get; set; } = null;
    public string? Prompt { get; set; }
    public string IpAddress { get; set; }
}
