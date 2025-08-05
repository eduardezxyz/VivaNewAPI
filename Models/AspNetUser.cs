using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? Email { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime? LockoutEndDateUtc { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public string UserName { get; set; } = null!;

    public bool? ResetPasswordOnLoginTf { get; set; }

    public virtual ICollection<AdminUser> AdminUsers { get; set; } = new List<AdminUser>();

    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    public virtual AspNetUserExtension? AspNetUserExtension { get; set; }

    public virtual ICollection<AspNetUserLogin> AspNetUserLogins { get; set; } = new List<AspNetUserLogin>();

    public virtual ICollection<GeneralContractorUser> GeneralContractorUsers { get; set; } = new List<GeneralContractorUser>();

    public virtual ICollection<ServiceUser> ServiceUsers { get; set; } = new List<ServiceUser>();

    public virtual ICollection<SubcontractorUser> SubcontractorUsers { get; set; } = new List<SubcontractorUser>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}
