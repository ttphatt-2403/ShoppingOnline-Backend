namespace ShoppingOnline.API.DTOs.Products
{
    public class ProductResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public decimal FinalPrice => Discount.HasValue ? Price - (Price * Discount.Value / 100) : Price;
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsInStock => StockQuantity > 0;
        
        // Related data
        public IEnumerable<ProductImageResponse> Images { get; set; } = new List<ProductImageResponse>();
        public IEnumerable<ProductVariantResponse> Variants { get; set; } = new List<ProductVariantResponse>();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class ProductImageResponse
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = null!;
    }

    public class ProductVariantResponse
    {
        public int VariantId { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public int StockQuantity { get; set; }
    }
}
