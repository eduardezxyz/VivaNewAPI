using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class GeneralContractor
{
    public int GeneralContractorId { get; set; }

    public string GeneralContractorName { get; set; } = null!;

    public string? VivaGeneralContractorId { get; set; }

    public int StatusId { get; set; }

    public string? JsonAttributes { get; set; }

    public DateTimeOffset CreateDt { get; set; }

    public DateTimeOffset? LastUpdateDt { get; set; }

    public string LastUpdateUser { get; set; } = null!;

    public DateTimeOffset? DeleteDt { get; set; }

    public string? LogoImage { get; set; }

    public string? DommainName { get; set; }

    public string? CreatedByUser { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<GeneralContractorUser> GeneralContractorUsers { get; set; } = new List<GeneralContractorUser>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
