namespace ShoppingOnline.API.DTOs.ProductImages
{
    public class ProductImageResponse
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        
        // Product details
        public string? ProductName { get; set; }
    }

    public class CreateProductImageRequest
    {
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class UpdateProductImageRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
    }
}
