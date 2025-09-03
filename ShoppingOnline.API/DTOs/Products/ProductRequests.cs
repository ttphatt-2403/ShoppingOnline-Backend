using System.ComponentModel.DataAnnotations;

namespace ShoppingOnline.API.DTOs.Products
{
    public class CreateProductRequest
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 255 characters")]
        public string ProductName { get; set; } = null!;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 99999999.99, ErrorMessage = "Price must be between 0.01 and 99,999,999.99")]
        public decimal Price { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100 percent")]
        public decimal Discount { get; set; } = 0;

        [Required(ErrorMessage = "Category ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be greater than 0")]
        public int CategoryId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; } = 0;
    }

    public class UpdateProductRequest : CreateProductRequest
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be greater than 0")]
        public int ProductId { get; set; }
    }
}
