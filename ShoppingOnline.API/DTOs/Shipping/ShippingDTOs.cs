namespace ShoppingOnline.API.DTOs.Shipping
{
    public class ShippingResponse
    {
        public int ShippingId { get; set; }
        public int OrderId { get; set; }
        public int? ShipperId { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime? ShippingDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Order details
        public string? OrderNumber { get; set; }
        public string? CustomerName { get; set; }
        
        // Shipper details
        public string? ShipperName { get; set; }
    }

    public class CreateShippingRequest
    {
        public int OrderId { get; set; }
        public int? ShipperId { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime? ShippingDate { get; set; }
        public string Status { get; set; } = "Preparing";
    }

    public class UpdateShippingRequest
    {
        public int? ShipperId { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime? ShippingDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ShippingStatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
        public DateTime? DeliveryDate { get; set; }
    }
}
