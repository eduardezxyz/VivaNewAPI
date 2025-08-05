using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class GeneralContractorUser
{
    public int GeneralContractorUserId { get; set; }

    public string UserId { get; set; } = null!;

    public int GeneralContractorId { get; set; }

    public bool CanApproveTf { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset LastUpdateDt { get; set; }

    public DateTimeOffset? DeleteDt { get; set; }

    public virtual GeneralContractor GeneralContractor { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
