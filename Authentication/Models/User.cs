using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Authentication.Models;
public class User : ApplicationUser
{
	[Required]
	[EmailAddress]
	public override string UserName { get; set; } = "";
	public virtual ICollection<Role>? Roles { get; set; }

	public User(User incoming)
	{
		Id = incoming.Id;
		FirstName = incoming.FirstName;
		LastName = incoming.LastName;
		UserName = incoming.UserName;
		PhoneNumber = incoming.PhoneNumber;
		EmailConfirmed = incoming.EmailConfirmed;
		EmailSent = incoming.EmailSent;
		LastPasswordReset = DateTime.SpecifyKind(incoming.LastPasswordReset, DateTimeKind.Utc);
		TwoFactorEnabled = incoming.TwoFactorEnabled;
		TwoFactorType = incoming.TwoFactorType;
		TwoFactorConfirmed = incoming.TwoFactorConfirmed;
		Roles = incoming.Roles;
	}

	public User() { }
}

public partial class UserDTO
{
	public string Id { get; set; } = "";
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public string UserName { get; set; } = "";
	public string Email { get; set; } = "";
	public string PhoneNumber { get; set; } = "";
	public string? CompanyName { get; set; }
	public string? JobTitle { get; set; }
	public bool EmailConfirmed { get; set; }
	public bool EmailSent { get; set; }
	public bool IsActive { get; set; }
	public string[]? Roles { get; set; }

	// Constructor that takes User
	public UserDTO(User incoming)
	{
		Id = incoming.Id;
		FirstName = incoming.FirstName;
		LastName = incoming.LastName;
		UserName = incoming.UserName ?? "";
		Email = incoming.Email ?? "";                 // Map Email
		PhoneNumber = incoming.PhoneNumber ?? "";
		CompanyName = incoming.CompanyName;           // Map CompanyName
		JobTitle = incoming.JobTitle;                 // Map JobTitle
		EmailConfirmed = incoming.EmailConfirmed;
		EmailSent = incoming.EmailSent;
		IsActive = incoming.IsActive;                 // Map IsActive

		if (incoming.Roles != null)
			Roles = incoming.Roles.Select(r => r.Name).ToArray();
	}
	
	// Add this constructor to handle ApplicationUser directly
    public UserDTO(ApplicationUser incoming)
    {
        Id = incoming.Id;
        FirstName = incoming.FirstName;
        LastName = incoming.LastName;
        UserName = incoming.UserName ?? "";
        Email = incoming.Email ?? "";
        PhoneNumber = incoming.PhoneNumber ?? "";
        CompanyName = incoming.CompanyName;
        JobTitle = incoming.JobTitle;
        EmailConfirmed = incoming.EmailConfirmed;
        EmailSent = incoming.EmailSent;
        IsActive = incoming.IsActive;
        Roles = new string[0]; // Will be populated separately if needed
    }

    public UserDTO() { }

}
