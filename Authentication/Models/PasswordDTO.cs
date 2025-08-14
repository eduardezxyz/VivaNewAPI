using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Authentication.Models;

public class PasswordDTO
{
    [Required(ErrorMessage = "Password is required")]
    public string NewPassword { get; set; } = "";
     [Required]
    public string Username { get; set; } = "";
}