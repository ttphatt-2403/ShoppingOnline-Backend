using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ShoppingOnline.API.Models;
using ShoppingOnline.API.DTOs.Categories;

namespace ShoppingOnline.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : BaseController
    {
        private readonly ShoppingDbContext _context;

        public CategoriesController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all categories with product count
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories()
        {
            return await _context.Categories
                .Select(c => new CategoryResponse
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    ProductCount = c.Products.Count()
                })
                .ToListAsync();
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponse>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryResponse
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description,
                    ProductCount = c.Products.Count()
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }

            return category;
        }

        /// <summary>
        /// Create new category (Admin/ProductManager only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<ActionResult<CategoryResponse>> CreateCategory(CreateCategoryRequest request)
        {
            // Validate ModelState
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            // Validate required parameters
            var paramValidation = ValidateRequiredParameters(
                (request.CategoryName, nameof(request.CategoryName))
            );
            if (paramValidation != null) return paramValidation;

            // Sanitize inputs
            request.CategoryName = SanitizeString(request.CategoryName);
            request.Description = string.IsNullOrWhiteSpace(request.Description) 
                ? null : SanitizeString(request.Description);

            // Validate category name length
            if (request.CategoryName.Length < 2)
            {
                return ErrorResponse("Category name must be at least 2 characters long");
            }

            // Check if category name already exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == request.CategoryName.ToLower());

            if (existingCategory != null)
            {
                return ErrorResponse("Category name already exists", null, 409);
            }

            var category = new Category
            {
                CategoryName = request.CategoryName,
                Description = request.Description
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var response = new CategoryResponse
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ProductCount = 0
            };

            return SuccessResponse(response, "Category created successfully");
        }

        /// <summary>
        /// Update category (Admin/ProductManager only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, UpdateCategoryRequest request)
        {
            if (id != request.CategoryId)
            {
                return BadRequest("ID trong URL không khớp với ID trong request");
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }

            // Check if new category name already exists (exclude current category)
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == request.CategoryName.ToLower() 
                                         && c.CategoryId != id);

            if (existingCategory != null)
            {
                return BadRequest("Tên danh mục đã tồn tại");
            }

            category.CategoryName = request.CategoryName;
            category.Description = request.Description;

            await _context.SaveChangesAsync();

            var response = new CategoryResponse
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ProductCount = await _context.Products.CountAsync(p => p.CategoryId == category.CategoryId)
            };

            return Ok(response);
        }

        /// <summary>
        /// Delete category (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }

            // Check if category has products
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest("Không thể xóa danh mục có chứa sản phẩm. Vui lòng xóa hoặc chuyển các sản phẩm trước.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get products in a category
        /// </summary>
        [HttpGet("{id}/products")]
        public async Task<ActionResult<IEnumerable<object>>> GetCategoryProducts(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }

            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Description,
                    p.Price,
                    p.Discount,
                    p.StockQuantity,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }
    }
}
