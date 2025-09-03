namespace ShoppingOnline.API.DTOs.Reviews
{
    public class ReviewResponse
    {
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; } = 5;
        public string? Comment { get; set; }
    }

    public class UpdateReviewRequest
    {
        public int Rating { get; set; } = 5;
        public string? Comment { get; set; }
    }
}
