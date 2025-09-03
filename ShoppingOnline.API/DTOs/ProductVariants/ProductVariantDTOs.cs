namespace ShoppingOnline.API.DTOs.ProductVariants
{
    public class ProductVariantResponse
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        
        // Product details
        public string? ProductName { get; set; }
        public decimal? ProductPrice { get; set; }
        public string? ProductImage { get; set; }
    }

    public class CreateProductVariantRequest
    {
        public int ProductId { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class UpdateProductVariantRequest
    {
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class UpdateStockRequest
    {
        public int StockQuantity { get; set; }
    }
}
