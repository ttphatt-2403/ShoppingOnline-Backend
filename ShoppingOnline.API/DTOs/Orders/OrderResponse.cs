using ShoppingOnline.API.Enums;

namespace ShoppingOnline.API.DTOs.Orders
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusDisplayName => Status.ToString();
        public decimal TotalAmount { get; set; }
        public string? ShippingAddress { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? ActualDelivery { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Notes { get; set; }
        public IEnumerable<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
        public PaymentResponse? Payment { get; set; }
        public ShippingResponse? Shipping { get; set; }
    }

    public class OrderItemResponse
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public string? ProductImageUrl { get; set; }
    }

    public class PaymentResponse
    {
        public int PaymentId { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime PaymentDate { get; set; }
        public string? TransactionId { get; set; }
    }

    public class ShippingResponse
    {
        public int ShippingId { get; set; }
        public string Address { get; set; } = null!;
        public string Method { get; set; } = null!;
        public decimal Cost { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? ActualDelivery { get; set; }
        public string? TrackingNumber { get; set; }
    }

    public class OrderPaginatedResponse
    {
        public IEnumerable<OrderResponse> Orders { get; set; } = new List<OrderResponse>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
