using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public string? ReportType { get; set; }

    public int? GeneratedBy { get; set; }

    public DateTime? GeneratedDate { get; set; }

    public string? FileUrl { get; set; }

    public string? DataJson { get; set; }

    public virtual User? GeneratedByNavigation { get; set; }
}
