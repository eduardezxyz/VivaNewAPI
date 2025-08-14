using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Authentication.Models;
public class User : ApplicationUser
{
	[Required]
	[EmailAddress]
	public override string UserName { get; set; } = "";
	public virtual ICollection<Role>? Roles { get; set; }

	public User(ApplicationUser incoming)
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
		//Roles = incoming.Roles;
	}

	public User() { }
}

public partial class UserDTO
{
	public string Id { get; set; }
	public string FirstName { get; set; }
	public string LastName { get; set; }
	public string UserName { get; set; }
	public string PhoneNumber { get; set; }
	public bool EmailConfirmed {get; set; }
	public bool EmailSent {get; set; }
	public string[]? Roles { get; set; }

	public UserDTO(User incoming)
	{
		Id = incoming.Id;
		FirstName = incoming.FirstName;
		LastName = incoming.LastName;
		UserName = incoming.UserName;
		PhoneNumber = incoming.PhoneNumber;
		EmailConfirmed = incoming.EmailConfirmed;
		EmailSent = incoming.EmailSent;
		if(incoming.Roles != null) Roles = incoming.Roles.Select(r => r.Name).ToArray();
	}

}
