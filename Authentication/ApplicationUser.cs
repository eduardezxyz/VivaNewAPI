using Microsoft.AspNetCore.Identity;

namespace NewVivaApi.Authentication;
public class ApplicationUser : IdentityUser<Guid>
{
	public string FirstName { get; set; } = "";
	public string LastName { get; set; } = "";
	public bool EmailSent { get; set; } = false;
	public DateTime LastPasswordReset { get; set; } = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);
	public string TwoFactorType { get; set; } = "";
	public bool TwoFactorConfirmed { get; set; } = false;
}
