using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class Subcontractor
{
    public int SubcontractorId { get; set; }

    public string SubcontractorName { get; set; } = null!;

    public string? VivaSubcontractorId { get; set; }

    public int StatusId { get; set; }

    public string? JsonAttributes { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public DateTimeOffset LastUpdateDt { get; set; }

    public string? LastUpdateUser { get; set; }

    public DateTimeOffset? DeleteDt { get; set; }

    public string? CreatedByUser { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<PayAppPayment> PayAppPayments { get; set; } = new List<PayAppPayment>();

    public virtual ICollection<SubcontractorProject> SubcontractorProjects { get; set; } = new List<SubcontractorProject>();

    public virtual ICollection<SubcontractorUser> SubcontractorUsers { get; set; } = new List<SubcontractorUser>();
}
