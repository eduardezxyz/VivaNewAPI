using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string VivaProjectId { get; set; } = null!;

    public int GeneralContractorId { get; set; }

    public int StatusId { get; set; }

    public DateTimeOffset StartDt { get; set; }

    public string? JsonAttributes { get; set; }

    public DateTime CreateDt { get; set; }

    public DateTime? LastUpdateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTime? DeleteDt { get; set; }

    public string? CreatedByUser { get; set; }

    public virtual GeneralContractor GeneralContractor { get; set; } = null!;

    public virtual ICollection<SubcontractorProject> SubcontractorProjects { get; set; } = new List<SubcontractorProject>();
}
