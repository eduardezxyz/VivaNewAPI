using System.ComponentModel.DataAnnotations;
using NewVivaApi.Authentication.Models;
using NewVivaApi.Models;
using NewVivaApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using NewVivaApi.Authentication;
using NewVivaApi.Services;
using System.Text.Json.Serialization;

namespace NewVivaApi.Models;
// Models used as parameters to AccountController actions.

public class AddExternalLoginBindingModel
{
    [Required]
    [Display(Name = "External access token")]
    public required string ExternalAccessToken { get; set; }
}

public class ChangePasswordBindingModel
{
    public string? UserID { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public required string OldPassword { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public required string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
}

public class RegisterBindingModel
{
    [Required]
    [Display(Name = "Email")]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public required string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
}

public class RegisterExternalBindingModel
{
    [Required]
    [Display(Name = "Email")]
    [EmailAddress]
    public required string Email { get; set; }
}

public class RemoveLoginBindingModel
{
    [Required]
    [Display(Name = "Login provider")]
    public required string LoginProvider { get; set; }

    [Required]
    [Display(Name = "Provider key")]
    public required string ProviderKey { get; set; }
}

public class SetPasswordBindingModel
{
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public required string NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
}


public class PasswordRequirements
{
    public bool RequireNumber { get; set; } = true;
    public bool RequireSymbol { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public int MinimumLength { get; set; } = 10;
    public int MaximumLength { get; set; } = 16;
}