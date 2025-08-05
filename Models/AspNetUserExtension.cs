using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class AspNetUserExtension
{
    public string Id { get; set; } = null!;

    public string? PasswordResetIdentity { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiration { get; set; }

    public virtual AspNetUser IdNavigation { get; set; } = null!;
}
