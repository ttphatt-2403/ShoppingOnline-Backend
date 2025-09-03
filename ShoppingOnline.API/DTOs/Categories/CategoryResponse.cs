namespace ShoppingOnline.API.DTOs.Categories
{
    public class CategoryResponse
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }
}
