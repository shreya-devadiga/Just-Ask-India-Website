// Models/AppUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JustAskIndia.Models;

public class AppUser : IdentityUser<int>
{
    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    public DateTime? LastLogin { get; set; }

    public string? FullName => $"{FirstName} {LastName}";
}
