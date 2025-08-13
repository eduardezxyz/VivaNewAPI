using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Authentication.Models;
public class ForgotPasswordModel
{
    [Required]
    [EmailAddress]
    public string UserName { get; set; } = "";
}