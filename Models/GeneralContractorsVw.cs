using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class GeneralContractorsVw
{
    public int GeneralContractorId { get; set; }

    public string GeneralContractorName { get; set; } = null!;

    public string? VivaGeneralContractorId { get; set; }

    public int StatusId { get; set; }

    public string? JsonAttributes { get; set; }

    public string? LogoImage { get; set; }

    public string? CreatedByUser { get; set; }

    public string? DommainName { get; set; }

    public decimal? NumSubs { get; set; }

    public decimal? Outstanding { get; set; }
}
