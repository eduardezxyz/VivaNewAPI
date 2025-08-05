using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class SubcontractorProject
{
    public int SubcontractorProjectId { get; set; }

    public int SubcontractorId { get; set; }

    public int ProjectId { get; set; }

    public decimal DiscountPct { get; set; }

    public string? JsonAttributes { get; set; }

    public int StatusId { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public DateTimeOffset LastUpdateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset? DeleteDt { get; set; }

    public string? CreatedByUser { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<PayApp> PayApps { get; set; } = new List<PayApp>();

    public virtual Project Project { get; set; } = null!;

    public virtual Subcontractor Subcontractor { get; set; } = null!;
}
