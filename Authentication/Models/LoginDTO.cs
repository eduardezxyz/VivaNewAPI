using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Authentication.Models;

public class LoginModel
{
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Required(ErrorMessage = "Username is required")]
    public string username { get; set; } = "";  // lowercase + default value

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string password { get; set; } = "";  // lowercase + default value

    // Only parameterless constructor for model binding
    public LoginModel() { }
}

public class ExternalLoginModel
{
    public string Token { get; set; } = "";
}