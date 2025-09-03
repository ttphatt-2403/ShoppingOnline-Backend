using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class Complaint
{
    public int ComplaintId { get; set; }

    public int? OrderId { get; set; }

    public int? UserId { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
