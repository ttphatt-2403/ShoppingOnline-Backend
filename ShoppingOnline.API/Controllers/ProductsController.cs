using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.Products;
using ShoppingOnline.API.Models;

namespace ShoppingOnline.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : BaseController
    {
        private readonly ShoppingDbContext _context;

        public ProductsController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all products with pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? search = null)
        {
            // Simple validation
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .AsQueryable();

                // Apply filters
                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.ProductName.Contains(search) || 
                                           (p.Description != null && p.Description.Contains(search)));
                }

                var totalItems = await query.CountAsync();
                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        productId = p.ProductId,
                        productName = p.ProductName,
                        description = p.Description,
                        price = p.Price,
                        discount = p.Discount,
                        stockQuantity = p.StockQuantity,
                        categoryId = p.CategoryId,
                        categoryName = p.Category != null ? p.Category.CategoryName : null,
                        images = p.ProductImages.Select(img => new
                        {
                            imageId = img.ImageId,
                            imageUrl = img.ImageUrl
                        }).ToList(),
                        createdAt = p.CreatedAt
                    })
                    .ToListAsync();

                var response = new
                {
                    products = products,
                    pagination = new
                    {
                        page = page,
                        pageSize = pageSize,
                        totalItems = totalItems,
                        totalPages = (int)Math.Ceiling((double)totalItems / pageSize)
                    }
                };

                return Ok(new
                {
                    Success = true,
                    Message = "Products retrieved successfully",
                    Data = response,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while retrieving products", ex.Message, 500);
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var validation = ValidateId(id);
            if (validation != null) return validation;

            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.Reviews)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                    return ErrorResponse("Product not found", statusCode: 404);

                var productDetail = new
                {
                    productId = product.ProductId,
                    productName = product.ProductName,
                    description = product.Description,
                    price = product.Price,
                    discount = product.Discount,
                    stockQuantity = product.StockQuantity,
                    categoryId = product.CategoryId,
                    categoryName = product.Category?.CategoryName,
                    images = product.ProductImages.Select(img => new
                    {
                        imageId = img.ImageId,
                        imageUrl = img.ImageUrl
                    }).ToList(),
                    variants = product.ProductVariants.Select(v => new
                    {
                        variantId = v.VariantId,
                        size = v.Size,
                        color = v.Color,
                        stockQuantity = v.StockQuantity
                    }).ToList(),
                    reviews = product.Reviews.Select(r => new
                    {
                        reviewId = r.ReviewId,
                        rating = r.Rating,
                        comment = r.Comment,
                        userName = r.User?.Username ?? "Anonymous",
                        createdAt = r.CreatedAt
                    }).OrderByDescending(r => r.createdAt).ToList(),
                    averageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating ?? 0) : 0,
                    totalReviews = product.Reviews.Count,
                    createdAt = product.CreatedAt
                };

                return Ok(new
                {
                    Success = true,
                    Message = "Product retrieved successfully",
                    Data = productDetail,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while retrieving the product", ex.Message, 500);
            }
        }

        /// <summary>
        /// Create new product (Admin/ProductManager only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Validate category exists
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId);
                if (!categoryExists)
                    return ErrorResponse("Category not found", statusCode: 404);

                // Check for duplicate product name
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower() == request.ProductName.ToLower());
                if (existingProduct != null)
                    return ErrorResponse("Product with this name already exists", statusCode: 409);

                var product = new Product
                {
                    ProductName = request.ProductName,
                    Description = request.Description,
                    Price = request.Price,
                    Discount = request.Discount,
                    StockQuantity = request.StockQuantity,
                    CategoryId = request.CategoryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var createdProduct = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

                var response = new
                {
                    productId = createdProduct!.ProductId,
                    productName = createdProduct.ProductName,
                    description = createdProduct.Description,
                    price = createdProduct.Price,
                    discount = createdProduct.Discount,
                    stockQuantity = createdProduct.StockQuantity,
                    categoryId = createdProduct.CategoryId,
                    categoryName = createdProduct.Category?.CategoryName,
                    createdAt = createdProduct.CreatedAt
                };

                return Ok(new
                {
                    Success = true,
                    Message = "Product created successfully",
                    Data = response,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while creating the product", ex.Message, 500);
            }
        }

        /// <summary>
        /// Update product (Admin/ProductManager only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            var validation = ValidateId(id);
            if (validation != null) return validation;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.ProductId != id)
                return ErrorResponse("Product ID mismatch");

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return ErrorResponse("Product not found", statusCode: 404);

                // Validate category exists
                var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == request.CategoryId);
                if (!categoryExists)
                    return ErrorResponse("Category not found", statusCode: 404);

                // Update product
                product.ProductName = request.ProductName;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Discount = request.Discount;
                product.StockQuantity = request.StockQuantity;
                product.CategoryId = request.CategoryId;

                await _context.SaveChangesAsync();

                var response = new
                {
                    productId = product.ProductId,
                    productName = product.ProductName,
                    description = product.Description,
                    price = product.Price,
                    discount = product.Discount,
                    stockQuantity = product.StockQuantity,
                    categoryId = product.CategoryId,
                    createdAt = product.CreatedAt
                };

                return Ok(new
                {
                    Success = true,
                    Message = "Product updated successfully",
                    Data = response,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while updating the product", ex.Message, 500);
            }
        }

        /// <summary>
        /// Delete product (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var validation = ValidateId(id);
            if (validation != null) return validation;

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return ErrorResponse("Product not found", statusCode: 404);

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Product deleted successfully",
                    Data = (object?)null,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while deleting the product", ex.Message, 500);
            }
        }
    }
}
