namespace NewVivaApi.Authentication.Models;
public class ChangePasswordDTO{
    public string OldPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}