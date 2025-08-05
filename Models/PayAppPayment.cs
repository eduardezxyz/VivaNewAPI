using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class PayAppPayment
{
    public int PaymentId { get; set; }

    public int PayAppId { get; set; }

    public int PaymentTypeId { get; set; }

    public int? SubcontractorId { get; set; }

    public decimal DollarAmount { get; set; }

    public string JsonAttributes { get; set; } = null!;

    public DateTimeOffset CreateDt { get; set; }

    public DateTimeOffset LastUpdateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset? DeleteDt { get; set; }

    public string? CreatedByUser { get; set; }

    public virtual PayApp PayApp { get; set; } = null!;

    public virtual Subcontractor? Subcontractor { get; set; }
}
