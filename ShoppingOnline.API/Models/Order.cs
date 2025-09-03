using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string? PaymentStatus { get; set; }

    public string? ShippingStatus { get; set; }

    public int? CreatedBy { get; set; }

    public int? AssignedShipperId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedShipper { get; set; }

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();

    public virtual User? User { get; set; }
}
