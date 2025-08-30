// Models/AppRole.cs
using Microsoft.AspNetCore.Identity;

namespace JustAskIndia.Models;

public class AppRole : IdentityRole<int>
{
    public AppRole() { }

    public AppRole(string roleName) : base(roleName) { }
}
