using System;
using System.Collections.Generic;
using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Models;

public partial class SubcontractorUser
{
    public int SubcontractorUserId { get; set; }

    public string UserId { get; set; } = null!;

    public int SubcontractorId { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset LastUpdateDt { get; set; }

    public DateTimeOffset? DeleteDt { get; set; }

    public virtual Subcontractor Subcontractor { get; set; } = null!;

    public virtual ApplicationUser User { get; set; } = null!;
}
