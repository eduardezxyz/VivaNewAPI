namespace NewVivaApi.Authentication.Models;

public class LoginResponse
{
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
    public string userName { get; set; }
    public string Issued { get; set; }
    public string Expires { get; set; }
}