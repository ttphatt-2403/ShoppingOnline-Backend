using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.ProductVariants;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductVariantsController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public ProductVariantsController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all product variants for a specific product
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ProductVariantResponse>>> GetProductVariants(int productId)
        {
            try
            {
                var variants = await _context.ProductVariants
                    .Include(v => v.Product)
                    .ThenInclude(p => p!.ProductImages)
                    .Where(v => v.ProductId == productId)
                    .Select(v => new ProductVariantResponse
                    {
                        VariantId = v.VariantId,
                        ProductId = v.ProductId ?? 0,
                        Size = v.Size ?? "",
                        Color = v.Color ?? "",
                        StockQuantity = v.StockQuantity ?? 0,
                        ProductName = v.Product != null ? v.Product.ProductName : "Unknown Product",
                        ProductPrice = v.Product != null ? v.Product.Price : 0,
                        ProductImage = v.Product != null && v.Product.ProductImages.Any() 
                            ? v.Product.ProductImages.First().ImageUrl 
                            : null
                    })
                    .ToListAsync();

                return Ok(variants);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving product variants", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all product variants (Admin only, with pagination)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProductVariantResponse>>> GetAllProductVariants([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var variants = await _context.ProductVariants
                    .Include(v => v.Product)
                    .ThenInclude(p => p!.ProductImages)
                    .OrderBy(v => v.ProductId)
                    .ThenBy(v => v.Size)
                    .ThenBy(v => v.Color)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(v => new ProductVariantResponse
                    {
                        VariantId = v.VariantId,
                        ProductId = v.ProductId ?? 0,
                        Size = v.Size ?? "",
                        Color = v.Color ?? "",
                        StockQuantity = v.StockQuantity ?? 0,
                        ProductName = v.Product != null ? v.Product.ProductName : "Unknown Product",
                        ProductPrice = v.Product != null ? v.Product.Price : 0,
                        ProductImage = v.Product != null && v.Product.ProductImages.Any() 
                            ? v.Product.ProductImages.First().ImageUrl 
                            : null
                    })
                    .ToListAsync();

                var totalCount = await _context.ProductVariants.CountAsync();

                var result = new
                {
                    variants = variants,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving product variants", details = ex.Message });
            }
        }

        /// <summary>
        /// Get product variant by ID
        /// </summary>
        [HttpGet("{variantId}")]
        public async Task<ActionResult<ProductVariantResponse>> GetProductVariant(int variantId)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .ThenInclude(p => p!.ProductImages)
                    .FirstOrDefaultAsync(v => v.VariantId == variantId);

                if (variant == null)
                    return NotFound(new { message = "Product variant not found" });

                var variantResponse = new ProductVariantResponse
                {
                    VariantId = variant.VariantId,
                    ProductId = variant.ProductId ?? 0,
                    Size = variant.Size ?? "",
                    Color = variant.Color ?? "",
                    StockQuantity = variant.StockQuantity ?? 0,
                    ProductName = variant.Product?.ProductName ?? "Unknown Product",
                    ProductPrice = variant.Product?.Price ?? 0m,
                    ProductImage = variant.Product?.ProductImages?.FirstOrDefault()?.ImageUrl
                };

                return Ok(variantResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the product variant", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new product variant (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductVariantResponse>> CreateProductVariant(CreateProductVariantRequest request)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Check if variant with same size and color already exists for this product
                var existingVariant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductId == request.ProductId && 
                                            v.Size == request.Size && 
                                            v.Color == request.Color);

                if (existingVariant != null)
                    return BadRequest(new { message = "Product variant with this size and color already exists" });

                var variant = new ProductVariant
                {
                    ProductId = request.ProductId,
                    Size = request.Size,
                    Color = request.Color,
                    StockQuantity = request.StockQuantity
                };

                _context.ProductVariants.Add(variant);
                await _context.SaveChangesAsync();

                var variantResponse = new ProductVariantResponse
                {
                    VariantId = variant.VariantId,
                    ProductId = variant.ProductId ?? 0,
                    Size = variant.Size ?? "",
                    Color = variant.Color ?? "",
                    StockQuantity = variant.StockQuantity ?? 0,
                    ProductName = product.ProductName ?? "Unknown Product",
                    ProductPrice = product.Price,
                    ProductImage = product.ProductImages?.FirstOrDefault()?.ImageUrl
                };

                return CreatedAtAction(nameof(GetProductVariant), new { variantId = variant.VariantId }, variantResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the product variant", details = ex.Message });
            }
        }

        /// <summary>
        /// Update product variant (Admin only)
        /// </summary>
        [HttpPut("{variantId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductVariantResponse>> UpdateProductVariant(int variantId, UpdateProductVariantRequest request)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .ThenInclude(p => p!.ProductImages)
                    .FirstOrDefaultAsync(v => v.VariantId == variantId);

                if (variant == null)
                    return NotFound(new { message = "Product variant not found" });

                // Check if another variant with same size and color exists for this product
                var existingVariant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductId == variant.ProductId && 
                                            v.Size == request.Size && 
                                            v.Color == request.Color &&
                                            v.VariantId != variantId);

                if (existingVariant != null)
                    return BadRequest(new { message = "Another product variant with this size and color already exists" });

                variant.Size = request.Size;
                variant.Color = request.Color;
                variant.StockQuantity = request.StockQuantity;

                await _context.SaveChangesAsync();

                var variantResponse = new ProductVariantResponse
                {
                    VariantId = variant.VariantId,
                    ProductId = variant.ProductId ?? 0,
                    Size = variant.Size ?? "",
                    Color = variant.Color ?? "",
                    StockQuantity = variant.StockQuantity ?? 0,
                    ProductName = variant.Product?.ProductName ?? "Unknown Product",
                    ProductPrice = variant.Product?.Price ?? 0m,
                    ProductImage = variant.Product?.ProductImages?.FirstOrDefault()?.ImageUrl
                };

                return Ok(variantResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the product variant", details = ex.Message });
            }
        }

        /// <summary>
        /// Update stock quantity (Admin only)
        /// </summary>
        [HttpPut("{variantId}/stock")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductVariantResponse>> UpdateStock(int variantId, UpdateStockRequest request)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .ThenInclude(p => p!.ProductImages)
                    .FirstOrDefaultAsync(v => v.VariantId == variantId);

                if (variant == null)
                    return NotFound(new { message = "Product variant not found" });

                if (request.StockQuantity < 0)
                    return BadRequest(new { message = "Stock quantity cannot be negative" });

                variant.StockQuantity = request.StockQuantity;
                await _context.SaveChangesAsync();

                var variantResponse = new ProductVariantResponse
                {
                    VariantId = variant.VariantId,
                    ProductId = variant.ProductId ?? 0,
                    Size = variant.Size ?? "",
                    Color = variant.Color ?? "",
                    StockQuantity = variant.StockQuantity ?? 0,
                    ProductName = variant.Product?.ProductName ?? "Unknown Product",
                    ProductPrice = variant.Product?.Price ?? 0m,
                    ProductImage = variant.Product?.ProductImages?.FirstOrDefault()?.ImageUrl
                };

                return Ok(variantResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating stock", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete product variant (Admin only)
        /// </summary>
        [HttpDelete("{variantId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteProductVariant(int variantId)
        {
            try
            {
                var variant = await _context.ProductVariants.FindAsync(variantId);

                if (variant == null)
                    return NotFound(new { message = "Product variant not found" });

                // Check if variant is used in any cart items or order items
                var hasCartItems = await _context.CartItems.AnyAsync(ci => ci.VariantId == variantId);
                var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.VariantId == variantId);

                if (hasCartItems || hasOrderItems)
                    return BadRequest(new { message = "Cannot delete product variant because it is referenced in cart items or order items" });

                _context.ProductVariants.Remove(variant);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product variant deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the product variant", details = ex.Message });
            }
        }

        /// <summary>
        /// Get available sizes for a product
        /// </summary>
        [HttpGet("product/{productId}/sizes")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableSizes(int productId)
        {
            try
            {
                var sizes = await _context.ProductVariants
                    .Where(v => v.ProductId == productId && v.StockQuantity > 0)
                    .Select(v => v.Size)
                    .Distinct()
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToListAsync();

                return Ok(sizes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving available sizes", details = ex.Message });
            }
        }

        /// <summary>
        /// Get available colors for a product
        /// </summary>
        [HttpGet("product/{productId}/colors")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableColors(int productId)
        {
            try
            {
                var colors = await _context.ProductVariants
                    .Where(v => v.ProductId == productId && v.StockQuantity > 0)
                    .Select(v => v.Color)
                    .Distinct()
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToListAsync();

                return Ok(colors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving available colors", details = ex.Message });
            }
        }

        /// <summary>
        /// Check stock availability for a specific variant
        /// </summary>
        [HttpGet("{variantId}/availability")]
        public async Task<ActionResult<object>> CheckAvailability(int variantId)
        {
            try
            {
                var variant = await _context.ProductVariants.FindAsync(variantId);

                if (variant == null)
                    return NotFound(new { message = "Product variant not found" });

                var availability = new
                {
                    variantId = variant.VariantId,
                    stockQuantity = variant.StockQuantity ?? 0,
                    isAvailable = (variant.StockQuantity ?? 0) > 0,
                    size = variant.Size,
                    color = variant.Color
                };

                return Ok(availability);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking availability", details = ex.Message });
            }
        }
    }
}
