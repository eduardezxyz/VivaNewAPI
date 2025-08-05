using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class SubcontractorProjectsVw
{
    public int SubcontractorProjectId { get; set; }

    public int SubcontractorId { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public int GeneralContractorId { get; set; }

    public decimal DiscountPct { get; set; }

    public string? JsonAttributes { get; set; }

    public int StatusId { get; set; }

    public string SubcontractorName { get; set; } = null!;

    public string? ContactEmail { get; set; }

    public string? Contact { get; set; }
}
