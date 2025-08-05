using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class PayAppHistory
{
    public int PayAppHistoryId { get; set; }

    public int PayAppId { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset LastUpdateDt { get; set; }

    public string Event { get; set; } = null!;

    public string LowestPermToView { get; set; } = null!;

    public virtual PayApp PayApp { get; set; } = null!;
}
