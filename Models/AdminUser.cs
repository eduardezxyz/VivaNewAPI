using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class AdminUser
{
    public int AdminUserId { get; set; }

    public string UserId { get; set; } = null!;

    public DateTimeOffset CreateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset LastUpdateDt { get; set; }

    public DateTimeOffset? DeleteDt { get; set; }

    public virtual AspNetUser User { get; set; } = null!;
}
