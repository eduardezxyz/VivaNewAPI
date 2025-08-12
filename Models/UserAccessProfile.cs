// UserType.cs
namespace NewVivaApi.Models
{
    public enum UserType
    {
        Viva,
        GeneralContractor,
        Subcontractor
    }
}

// UserAccessProfile.cs
namespace NewVivaApi.Models
{
    public class UserAccessProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public bool CanApproveTf { get; set; }
        public int? GeneralContractorId { get; set; }
        public int? SubcontractorId { get; set; }
        public bool ResetPasswordOnLogin { get; set; }

        public UserAccessProfile()
        {
            CanApproveTf = false;
        }
    }
}