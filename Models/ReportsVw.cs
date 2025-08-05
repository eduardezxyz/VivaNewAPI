using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class ReportsVw
{
    public int ReportId { get; set; }

    public string ReportName { get; set; } = null!;

    public string VivaReportId { get; set; } = null!;

    public DateTime CreateDt { get; set; }
}
