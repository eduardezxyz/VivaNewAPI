using System;
using System.Collections.Generic;
using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Models;

public partial class ServiceUser
{
    public int ServiceUserId { get; set; }

    public string UserId { get; set; } = null!;

    public DateTimeOffset CreateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset LastUpdateDt { get; set; }

    public DateTimeOffset? DeleteDt { get; set; }

    public string? WebHookUrl { get; set; }

    public string? BearerToken { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
