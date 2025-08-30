using JustAskIndia.Data;
using JustAskIndia.DTOs;
using JustAskIndia.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using JustAskIndia.Utilities;

namespace JustAskIndia.Services;


//ToDo: Implement GetAppUsersByUserName and change all the calls to increase the speed
public class CacheService(IMemoryCache cache, AppDbContext context)
{
    private readonly IMemoryCache _cache = cache;
    private readonly AppDbContext _context = context;

    public async ValueTask<List<AppUser>> GetAppUsersAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue("APP_USERS", out List<AppUser>? appUsersFromCache))
        {
            return appUsersFromCache ?? [];
        }

        var appUsers = await _context.Users.OrderBy(e => e.Id).ToListAsync(ct);

        _cache.Set("APP_USERS", appUsers, TimeSpan.FromHours(Constants.CacheRefreshTimeInHours));

        return appUsers;
    }

    public async ValueTask<Dictionary<int, AppUser>> GetAppUsersToFindByIdAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue("APP_USERS_BY_ID", out Dictionary<int, AppUser>? appUsersFromCache))
        {
            return appUsersFromCache ?? [];
        }

        var appUsers = await GetAppUsersAsync(ct);

        Dictionary<int, AppUser> appUserDict = [];
        if (appUsers != null)
        {
            foreach (var appUser in appUsers)
            {
                appUserDict.Add(appUser.Id, appUser);
            }
        }

        _cache.Set("APP_USERS_BY_ID", appUserDict, TimeSpan.FromHours(Constants.CacheRefreshTimeInHours));

        return appUserDict;
    }

    public async ValueTask<Dictionary<string, AppUser>> GetAppUsersToFindByUserNameAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue("APP_USERS_BY_NAME", out Dictionary<string, AppUser>? appUsersFromCache))
        {
            return appUsersFromCache ?? [];
        }

        var appUsers = await GetAppUsersAsync(ct);

        Dictionary<string, AppUser> appUserDict = [];
        if (appUsers != null)
        {
            foreach (var appUser in appUsers)
            {
                if (appUser.UserName is null) continue;

                appUserDict.Add(appUser.UserName, appUser);
            }
        }

        _cache.Set("APP_USERS_BY_NAME", appUserDict, TimeSpan.FromHours(Constants.CacheRefreshTimeInHours));

        return appUserDict;
    }

    public async Task<List<CachedUserDTO>> GetAppUsersWithRolesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue("APP_USERS_AND_ROLES", out List<CachedUserDTO>? appUsersFromCache))
        {
            if (appUsersFromCache is not null)
                return appUsersFromCache;
        }

        var cachedUsers = new List<CachedUserDTO>();

        var roles = await _context.Roles.ToListAsync(ct);
        var dictRoles = new Dictionary<int, string>();
        foreach (var role in roles)
        {
            dictRoles.Add(role.Id, role.Name);
        }

        var users = await GetAppUsersAsync(ct);
        if (users is null)
            return [];

        var userRoles = await _context.UserRoles.ToListAsync(ct);
        foreach (var user in users)
        {
            var thisUserRole = userRoles.FirstOrDefault(u => u.UserId == user.Id);
            if (thisUserRole is null)
                continue;

            cachedUsers.Add(new CachedUserDTO
            { Id = user.Id, AppUser = user, Role = dictRoles[thisUserRole.RoleId] });
        }

        _cache.Set("APP_USERS_AND_ROLES", cachedUsers, TimeSpan.FromHours(Constants.CacheRefreshTimeInHours));

        return cachedUsers;
    }

    public void ResetCachedUsersAndRoles()
    {
        _cache.Remove("APP_USERS");
        _cache.Remove("APP_USERS_BY_ID");
        _cache.Remove("APP_USERS_BY_NAME");
        _cache.Remove("APP_USERS_AND_ROLES");
    }
}
