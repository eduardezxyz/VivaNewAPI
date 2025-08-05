using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class UserProfile
{
    public string UserId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset LastUpdateDt { get; set; }

    public DateTimeOffset? DeleteDt { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
