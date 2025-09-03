namespace ShoppingOnline.API.DTOs.Products
{
    public class ProductSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; } = false;
        public string SortBy { get; set; } = "name"; // name, price, created_date, rating
        public string SortOrder { get; set; } = "asc"; // asc, desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ProductPaginatedResponse
    {
        public IEnumerable<ProductResponse> Products { get; set; } = new List<ProductResponse>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
