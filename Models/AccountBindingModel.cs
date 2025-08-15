using System.ComponentModel.DataAnnotations;

public class RegisterSystemUserModel
{
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }

    // These will be set by the service
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
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