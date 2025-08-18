namespace  NewVivaApi.Models;

public enum UserType
{
    Viva,
    GeneralContractor,
    Subcontractor
}

public class UserAccessProfile
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public bool CanApproveTF { get; set; } = false;
    public int? GeneralContractorID { get; set; }
    public int? SubcontractorID { get; set; }
    public bool ResetPasswordOnLogin { get; set; } = false;
}
