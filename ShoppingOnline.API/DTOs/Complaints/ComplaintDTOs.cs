namespace ShoppingOnline.API.DTOs.Complaints
{
    public class ComplaintResponse
    {
        public int ComplaintId { get; set; }
        public int? OrderId { get; set; }
        public int UserId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Order details
        public string? OrderNumber { get; set; }
        
        // User details
        public string UserName { get; set; } = string.Empty;
    }

    public class CreateComplaintRequest
    {
        public int? OrderId { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateComplaintStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
