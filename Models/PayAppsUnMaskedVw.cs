using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class PayAppsUnMaskedVw
{
    public int PayAppId { get; set; }

    public string? VivaPayAppId { get; set; }

    public int SubcontractorProjectId { get; set; }

    public int ProjectId { get; set; }

    public int SubcontractorId { get; set; }

    public int GeneralContractorId { get; set; }

    public decimal RequestedAmount { get; set; }

    public decimal? ApprovedAmount { get; set; }

    public int StatusId { get; set; }

    public string? JsonAttributes { get; set; }

    public DateTimeOffset? ApprovalDt { get; set; }
}
