using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Authentication.Models;
public class PasswordDTO
{
    [Required]
    public string NewPassword { get; set; } = "";
}