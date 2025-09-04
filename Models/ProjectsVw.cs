using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NewVivaApi.Models;

public partial class ProjectsVw
{
    public int ProjectId { get; set; }

    public string VivaProjectId { get; set; } = null!;

    public int GeneralContractorId { get; set; }

    public DateTimeOffset StartDt { get; set; }

    public int StatusId { get; set; }

    public string? JsonAttributes { get; set; }

    public string ProjectName { get; set; } = null!;

    public decimal? UnpaidBalance { get; set; }
}
