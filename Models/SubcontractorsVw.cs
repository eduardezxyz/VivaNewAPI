using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class SubcontractorsVw
{
    public int SubcontractorId { get; set; }

    public string SubcontractorName { get; set; } = null!;

    public string? VivaSubcontractorId { get; set; }

    public int StatusId { get; set; }

    public string? CreatedByUser { get; set; }

    public string? JsonAttributes { get; set; }
}
