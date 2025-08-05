using System;
using System.Collections.Generic;

namespace NewVivaApi.Models;

public partial class Document
{
    public int DocumentId { get; set; }

    public int DocumentTypeId { get; set; }

    public string Bucket { get; set; } = null!;

    public string Path { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string DownloadFileName { get; set; } = null!;

    public DateTimeOffset CreateDt { get; set; }

    public string CreateByUser { get; set; } = null!;

    public DateTimeOffset? DeleteDt { get; set; }

    public int? PayAppId { get; set; }

    public int? SubcontractorId { get; set; }

    public int? SubcontractorProjectId { get; set; }

    public int? GeneralContractorId { get; set; }

    public virtual GeneralContractor? GeneralContractor { get; set; }

    public virtual PayApp? PayApp { get; set; }

    public virtual Subcontractor? Subcontractor { get; set; }

    public virtual SubcontractorProject? SubcontractorProject { get; set; }
}
