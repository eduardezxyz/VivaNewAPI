using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Authentication;
public class LoginModel
{
    [EmailAddress]
    [Required(ErrorMessage = "User Name is required")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = "";

    public LoginModel (string username, string password) {
        Username = username;
        Password = password;
    }
}