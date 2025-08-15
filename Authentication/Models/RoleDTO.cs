using Microsoft.AspNetCore.Identity;

namespace NewVivaApi.Authentication.Models;
public class RoleDTO
{
	public string Id { get; set; }
    public string Name { get; set; } = "";
	public List<string>? Users { get; set; }


	public RoleDTO(Role role, List<ApplicationUser> usersWithRole)
	{
		Id = role.Id;
		Name = role.Name;
		var userList = new List<string>();
        foreach (var user in usersWithRole)
        {
            userList.Add(user.UserName);
        }
        Users = userList;
	}

	public RoleDTO() { }
}
