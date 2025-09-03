using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.Models;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsSimpleController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public ProductsSimpleController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all products
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Description,
                        p.Price,
                        p.Discount,
                        p.StockQuantity,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "No Category",
                        Images = p.ProductImages.Select(img => img.ImageUrl).ToList(),
                        IsInStock = p.StockQuantity > 0
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving products", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .Where(p => p.ProductId == id)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Description,
                        p.Price,
                        p.Discount,
                        p.StockQuantity,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "No Category",
                        Images = p.ProductImages.Select(img => img.ImageUrl).ToList(),
                        Variants = p.ProductVariants.Select(v => new
                        {
                            v.VariantId,
                            v.Size,
                            v.Color,
                            v.StockQuantity
                        }).ToList(),
                        IsInStock = p.StockQuantity > 0
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(new { message = $"Product with ID {id} not found" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the product", details = ex.Message });
            }
        }

        /// <summary>
        /// Get products by category
        /// </summary>
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Where(p => p.CategoryId == categoryId)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Description,
                        p.Price,
                        p.Discount,
                        p.StockQuantity,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "No Category",
                        Images = p.ProductImages.Select(img => img.ImageUrl).ToList(),
                        IsInStock = p.StockQuantity > 0
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving products by category", details = ex.Message });
            }
        }
    }
}
