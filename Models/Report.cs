using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public string ReportName { get; set; } = null!;

    public string VivaReportId { get; set; } = null!;

    public DateTime CreateDt { get; set; }

    public DateTime? LastUpdateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTime? DeleteDt { get; set; }
}
