using Microsoft.AspNetCore.Identity;

namespace NewVivaApi.Authentication.Models;
public class Role : IdentityRole<Guid>
{
	public ICollection<User>? Users { get; set; }

	public Role(Role role)
	{
		Id = role.Id;
		Name = role.Name;
		Users = role.Users;
	}

	public Role() { }
}
