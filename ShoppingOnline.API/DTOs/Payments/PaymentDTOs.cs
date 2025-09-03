namespace ShoppingOnline.API.DTOs.Payments
{
    public class PaymentResponse
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        
        // Order details
        public string? OrderNumber { get; set; }
        public string? CustomerName { get; set; }
    }

    public class CreatePaymentRequest
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class UpdatePaymentRequest
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PaymentStatusUpdateRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
