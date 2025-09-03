using System;
using System.Collections.Generic;

namespace ShoppingOnline.API.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public int? VariantId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtOrder { get; set; }

    public string? ProductName { get; set; }

    public string? VariantName { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Product? Product { get; set; }

    public virtual ProductVariant? Variant { get; set; }
}
