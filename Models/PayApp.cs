using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class PayApp
{
    public int PayAppId { get; set; }

    public string? VivaPayAppId { get; set; }

    public int SubcontractorProjectId { get; set; }

    public int StatusId { get; set; }

    public decimal RequestedAmount { get; set; }

    public decimal? ApprovedAmount { get; set; }

    public string? JsonAttributes { get; set; }

    public string? HistoryAttributes { get; set; }

    public DateTimeOffset? ApprovalDt { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public DateTimeOffset LastUpdateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset? DeleteDt { get; set; }

    public string? CreatedByUser { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<PayAppHistory> PayAppHistories { get; set; } = new List<PayAppHistory>();

    public virtual ICollection<PayAppPayment> PayAppPayments { get; set; } = new List<PayAppPayment>();

    public virtual SubcontractorProject SubcontractorProject { get; set; } = null!;
}
