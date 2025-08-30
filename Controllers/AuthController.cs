using JustAskIndia.Data;
using JustAskIndia.DTOs;
using JustAskIndia.Models;
using JustAskIndia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JustAskIndia.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    //private readonly IEmailSender _emailService;


    public AuthController(
        AuthService authService,
        AppDbContext context,
        UserManager<AppUser> userManager)
    {
        _authService = authService;
        _context = context;
        _userManager = userManager;
       
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDTO dto)
    {
        (var isSuccess, var message) = await _authService.RegisterAsync(dto);

        var result = new ResultDTO<string>(isSuccess, message);

        return new JsonResult(result);
    }

    [Authorize]
    [HttpPost("update")]
    public async Task<IActionResult> Update(AppUserUpdateDTO dto)
    {
        (var isSuccess, var message) = await _authService.UpdateAsync(dto);

        var result = new ResultDTO<string>(isSuccess, message);

        return new JsonResult(result);
    }

    [Authorize]
    [HttpPost("update_role")]
    public async Task<IActionResult> UpdateRole(AppUserUpdateRole dto)
    {
        (var isSuccess, var message) = await _authService.UpdateRoleAsync(dto);

        var result = new ResultDTO<string>(isSuccess, message);

        return new JsonResult(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        (var isSuccess, var message, var data) = await _authService.LoginAsync(dto);

        var result = new ResultDTO<string>(isSuccess, message, data);

        return new JsonResult(result);
    }

    //[AllowAnonymous]
    //[HttpPost("generate_otp")]
    //public async Task<IActionResult> GenerateOtp(OtpLoginDTO dto)
    //{
    //    (var isSuccess, var message) = await _authService.GenerateOtpAsync(dto);

    //    var result = new ResultDTO<string>(isSuccess, message);

    //    return new JsonResult(result);
    //}

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshLogin()
    {
        if (User.Identity is null || User.Identity.Name is null)
        {
            return new JsonResult(new ResultDTO<string>(false, "Session Timed-out"));
        }

        (var isSuccess, var message, var data) = await _authService.RefreshLoginAsync(User.Identity?.Name);

        var result = new ResultDTO<string>(isSuccess, message, data);

        return new JsonResult(result);
    }

    [Authorize]
    [HttpPost("reset_password")]
    public async Task<IActionResult> ResetPassword(AppUserResetPassword dto)
    {
        (var isSuccess, var message) = await _authService.ResetPassword(dto);

        var result = new ResultDTO<string>(isSuccess, message);

        return new JsonResult(result);
    }

    [Authorize]
    [HttpGet("user_info/{username}")]
    public async Task<IActionResult> UserInfo(string? username)
    {
        if (username is null)
        {
            return new JsonResult(new ResultDTO<string>(false, "Invalid Inputs"));
        }

        (var isSuccess, var message, var data) = await _authService.GetUserInfoAsync(username);

        var result = new ResultDTO<AppUserDTO?>(isSuccess, message, data);

        return new JsonResult(result);
    }

    [Authorize]
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        (var isSuccess, var message) = await _authService.DeleteAsync(id);

        var result = new ResultDTO<string>(isSuccess, message);

        return new JsonResult(result);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        (var isSuccess, var message, var data) = await _authService.GetUsersAsync(ct);

        var result = new ResultDTO<List<AppUserDTO>>(isSuccess, message, data);

        return new JsonResult(result);
    }

    [Authorize]
    [HttpPost("create_role")]
    public async Task<IActionResult> CreateNewRole(AppRoleCreate dto)
    {
        (var isSuccess, var message) = await _authService.CreateRoleAsync(dto);

        var result = new ResultDTO<string>(isSuccess, message);

        return new JsonResult(result);
    }

    //[Authorize]
    //[HttpGet("users_paged")]
    //public async Task<IActionResult> GetUsersPaged(AppUsersPageModel model, CancellationToken ct)
    //{
    //    (var isSuccess, var message, var data) = await _authService.GetUsersAsync(model);

    //    var result = new ResultDTO<AppUsersPageDTO>(isSuccess, message, data);

    //    return new JsonResult(result);
    //}

    [Authorize]
    [HttpGet("users_emails")]
    public async Task<IActionResult> GetUsersEmails(CancellationToken ct)
    {
        if (User.Identity is null || User.Identity.Name is null)
        {
            return new JsonResult(new ResultDTO<string>(false, "Session Timed-out"));
        }

        var emails = await _authService.GetUsersEmailsAsync(User.Identity.Name);

        return emails.Count > 0
            ? new JsonResult(new ResultDTO<List<string>>(true, "", emails))
            : new JsonResult(new ResultDTO<List<string>>(false, "Could'nt fetch the Emails. Try Again"));
    }

    //[AllowAnonymous]
    //[HttpPost("reset_password_self")]
    //public async Task<IActionResult> ForgotPassword(AppUserPasswordResetSelf dto)
    //{
    //    if (string.IsNullOrEmpty(dto.Email))
    //    {
    //        return new JsonResult(new ResultDTO<string>(false, "Invalid Email or User does not exists"));
    //    }

    //    var user = await _context.Users
    //        .FirstOrDefaultAsync(e => e.Email.ToUpper() == dto.Email.ToUpper());

    //    if (user is null)
    //    {
    //        return new JsonResult(new ResultDTO<string>(false, "User does not exists. Try Again"));
    //    }

    //    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    //    var callback = Constants.ThisUrl + Url.Page($"/account/resetpassword", new { token, email = user.Email });

    //    var result = await _emailService.SendResetPasswordEmail(dto.Email, callback);

    //    return result
    //        ? new JsonResult(new ResultDTO<string>(true, "An Email has been sent with the Reset Link"))
    //        : new JsonResult(new ResultDTO<string>(false, "Something went wrong while sending Email. Try again"));
    //}


    //[AllowAnonymous]
    //[HttpPost("change_file_paths")]
    //public async Task<IActionResult> ChangeFilePaths()
    //{
    //    var cvsToChange = await _context.Cvs
    //        .Where(e => e.DocumentPath.StartsWith("c:"))
    //        .ToListAsync();

    //    foreach (var cv in cvsToChange)
    //    {
    //        cv.DocumentPath = cv.DocumentPath
    //            .Replace('\\', '/')
    //            .Replace("c:/RECRUITER", "/home/webuser/recruiter");
    //    }

    //    var jdsToChange = await _context.Jds
    //        .Where(e => e.HasDocument && e.DocumentPath != null && e.DocumentPath.StartsWith("c:"))
    //        .ToListAsync();

    //    foreach (var jd in jdsToChange)
    //    {
    //        jd.DocumentPath = jd.DocumentPath
    //            .Replace('\\', '/')
    //            .Replace("c:/RECRUITER", "/home/webuser/recruiter");
    //    }

    //    var result = await _context.SaveChangesAsync();

    //    return result > 0
    //        ? new JsonResult(new ResultDTO<string>(true, "Successfully Changed the File Paths"))
    //        : new JsonResult(new ResultDTO<string>(false, "Failed Changed the File Paths"));
    //}
}
