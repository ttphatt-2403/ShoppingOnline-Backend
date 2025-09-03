using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class Shipping
{
    public int ShippingId { get; set; }

    public int? OrderId { get; set; }

    public int? ShipperId { get; set; }

    public string? ShippingAddress { get; set; }

    public DateTime? ShippingDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public string? Status { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? Shipper { get; set; }
}
