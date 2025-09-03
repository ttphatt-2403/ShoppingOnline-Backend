using System.ComponentModel.DataAnnotations;
using ShoppingOnline.API.Enums;

namespace ShoppingOnline.API.DTOs.Orders
{
    public class CreateOrderRequest
    {
        [Required(ErrorMessage = "Shipping address is required")]
        [StringLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters")]
        public string ShippingAddress { get; set; } = null!;

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; } = null!;

        public string? Notes { get; set; }

        // Optional: Create order from specific cart items (if not provided, uses entire cart)
        public List<int>? CartItemIds { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        public OrderStatus Status { get; set; }

        public string? Notes { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
    }

    public class OrderSearchRequest
    {
        public OrderStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? SearchTerm { get; set; } // Search in user name, email, or order ID
        public string SortBy { get; set; } = "order_date"; // order_date, total_amount, status
        public string SortOrder { get; set; } = "desc"; // asc, desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
