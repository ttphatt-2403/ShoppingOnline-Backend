namespace ShoppingOnline.API.DTOs.Cart
{
    public class CartResponse
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public IEnumerable<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();
        public decimal TotalAmount => Items.Sum(item => item.TotalPrice);
        public int TotalItems => Items.Sum(item => item.Quantity);
    }

    public class CartItemResponse
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal ProductPrice { get; set; }
        public decimal? ProductDiscount { get; set; }
        public decimal FinalPrice => ProductDiscount.HasValue 
            ? ProductPrice - (ProductPrice * ProductDiscount.Value / 100) 
            : ProductPrice;
        public int Quantity { get; set; }
        public decimal TotalPrice => FinalPrice * Quantity;
        public DateTime AddedAt { get; set; }
        public string? ProductImageUrl { get; set; }
        public bool IsInStock { get; set; }
        public int AvailableStock { get; set; }
    }
}
