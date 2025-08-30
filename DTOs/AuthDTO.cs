namespace JustAskIndia.DTOs;

public record LoginDTO(string Username, string Password);

public record OtpLoginDTO(string Email, string Phone);
//public record AppUserChangePassword(int Id, string OldPassword, string NewPassword);

//public record AppUserResetPassword(int Id, string Password);

//public record AppUserPasswordResetSelf(string Email);

//public record AppUserPasswordResetSelfLink(string Email, string Token);

public record RegisterDTO(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string UserName,
    string Password,
    string Role);

public record AppUserUpdateDTO(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string UserName,
    string PhoneNumber
);

public record AppRoleCreate(string Role);

public record AppUserUpdateRole(int Id, string OldRole, string NewRole);

public record AppUserChangePassword(int Id, string OldPassword, string NewPassword);

public record AppUserResetPassword(int Id, string Password);

public record AppUserPasswordResetSelf(string Email);

public record AppUserPasswordResetSelfLink(string Email, string Token);

public record AppUsersPageModel(string Page, string ItemsPerPage, string SortBy);

public record AppUsersPageDTO(string Items, string Total);

public record AppUserDTO(
    int Id,
    string FullName,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string UserName,
    DateTime? LastLogin,
    string? Role);

