using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Authentication.Models;
public class GroupDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<User>? Users { get; set; } = new List<User>();
}