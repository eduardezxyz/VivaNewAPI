using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class UserProfilesVw
{
    public string UserId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? UserType { get; set; }

    public int? GeneralContractorId { get; set; }

    public int? SubcontractorId { get; set; }

    public string? CompanyName { get; set; }

    public string UserStatus { get; set; } = null!;

    public string Password { get; set; } = null!;
}
