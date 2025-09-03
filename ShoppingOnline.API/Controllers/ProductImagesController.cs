using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.ProductImages;
using ShoppingOnline.API.Models;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductImagesController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public ProductImagesController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all images for a specific product
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ProductImageResponse>>> GetProductImages(int productId)
        {
            try
            {
                var images = await _context.ProductImages
                    .Include(img => img.Product)
                    .Where(img => img.ProductId == productId)
                    .Select(img => new ProductImageResponse
                    {
                        ImageId = img.ImageId,
                        ProductId = img.ProductId ?? 0,
                        ImageUrl = img.ImageUrl,
                        ProductName = img.Product != null ? img.Product.ProductName : "Unknown Product"
                    })
                    .ToListAsync();

                return Ok(images);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving product images", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all product images (Admin only, with pagination)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProductImageResponse>>> GetAllProductImages([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var images = await _context.ProductImages
                    .Include(img => img.Product)
                    .OrderBy(img => img.ProductId)
                    .ThenBy(img => img.ImageId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(img => new ProductImageResponse
                    {
                        ImageId = img.ImageId,
                        ProductId = img.ProductId ?? 0,
                        ImageUrl = img.ImageUrl,
                        ProductName = img.Product != null ? img.Product.ProductName : "Unknown Product"
                    })
                    .ToListAsync();

                var totalCount = await _context.ProductImages.CountAsync();

                var result = new
                {
                    images = images,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving product images", details = ex.Message });
            }
        }

        /// <summary>
        /// Get product image by ID
        /// </summary>
        [HttpGet("{imageId}")]
        public async Task<ActionResult<ProductImageResponse>> GetProductImage(int imageId)
        {
            try
            {
                var image = await _context.ProductImages
                    .Include(img => img.Product)
                    .FirstOrDefaultAsync(img => img.ImageId == imageId);

                if (image == null)
                    return NotFound(new { message = "Product image not found" });

                var imageResponse = new ProductImageResponse
                {
                    ImageId = image.ImageId,
                    ProductId = image.ProductId ?? 0,
                    ImageUrl = image.ImageUrl,
                    ProductName = image.Product?.ProductName ?? "Unknown Product"
                };

                return Ok(imageResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the product image", details = ex.Message });
            }
        }

        /// <summary>
        /// Add a new image to a product (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductImageResponse>> CreateProductImage(CreateProductImageRequest request)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Validate image URL
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                    return BadRequest(new { message = "Image URL is required" });

                // Check if image URL already exists for this product
                var existingImage = await _context.ProductImages
                    .FirstOrDefaultAsync(img => img.ProductId == request.ProductId && img.ImageUrl == request.ImageUrl);

                if (existingImage != null)
                    return BadRequest(new { message = "This image URL already exists for this product" });

                var productImage = new ProductImage
                {
                    ProductId = request.ProductId,
                    ImageUrl = request.ImageUrl
                };

                _context.ProductImages.Add(productImage);
                await _context.SaveChangesAsync();

                var imageResponse = new ProductImageResponse
                {
                    ImageId = productImage.ImageId,
                    ProductId = productImage.ProductId ?? 0,
                    ImageUrl = productImage.ImageUrl,
                    ProductName = product.ProductName ?? "Unknown Product"
                };

                return CreatedAtAction(nameof(GetProductImage), new { imageId = productImage.ImageId }, imageResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the product image", details = ex.Message });
            }
        }

        /// <summary>
        /// Update product image (Admin only)
        /// </summary>
        [HttpPut("{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductImageResponse>> UpdateProductImage(int imageId, UpdateProductImageRequest request)
        {
            try
            {
                var image = await _context.ProductImages
                    .Include(img => img.Product)
                    .FirstOrDefaultAsync(img => img.ImageId == imageId);

                if (image == null)
                    return NotFound(new { message = "Product image not found" });

                // Validate image URL
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                    return BadRequest(new { message = "Image URL is required" });

                // Check if the new image URL already exists for this product (excluding current image)
                var existingImage = await _context.ProductImages
                    .FirstOrDefaultAsync(img => img.ProductId == image.ProductId && 
                                              img.ImageUrl == request.ImageUrl && 
                                              img.ImageId != imageId);

                if (existingImage != null)
                    return BadRequest(new { message = "This image URL already exists for this product" });

                image.ImageUrl = request.ImageUrl;
                await _context.SaveChangesAsync();

                var imageResponse = new ProductImageResponse
                {
                    ImageId = image.ImageId,
                    ProductId = image.ProductId ?? 0,
                    ImageUrl = image.ImageUrl,
                    ProductName = image.Product?.ProductName ?? "Unknown Product"
                };

                return Ok(imageResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the product image", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete product image (Admin only)
        /// </summary>
        [HttpDelete("{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteProductImage(int imageId)
        {
            try
            {
                var image = await _context.ProductImages.FindAsync(imageId);

                if (image == null)
                    return NotFound(new { message = "Product image not found" });

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product image deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the product image", details = ex.Message });
            }
        }

        /// <summary>
        /// Get primary image for a product (first image)
        /// </summary>
        [HttpGet("product/{productId}/primary")]
        public async Task<ActionResult<ProductImageResponse>> GetPrimaryImage(int productId)
        {
            try
            {
                var image = await _context.ProductImages
                    .Include(img => img.Product)
                    .Where(img => img.ProductId == productId)
                    .OrderBy(img => img.ImageId)
                    .FirstOrDefaultAsync();

                if (image == null)
                    return NotFound(new { message = "No images found for this product" });

                var imageResponse = new ProductImageResponse
                {
                    ImageId = image.ImageId,
                    ProductId = image.ProductId ?? 0,
                    ImageUrl = image.ImageUrl,
                    ProductName = image.Product?.ProductName ?? "Unknown Product"
                };

                return Ok(imageResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the primary image", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk upload images for a product (Admin only)
        /// </summary>
        [HttpPost("product/{productId}/bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProductImageResponse>>> BulkUploadImages(int productId, [FromBody] List<string> imageUrls)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return NotFound(new { message = "Product not found" });

                if (imageUrls == null || !imageUrls.Any())
                    return BadRequest(new { message = "At least one image URL is required" });

                // Validate all URLs
                var validUrls = imageUrls.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
                if (!validUrls.Any())
                    return BadRequest(new { message = "No valid image URLs provided" });

                // Check for existing URLs
                var existingUrls = await _context.ProductImages
                    .Where(img => img.ProductId == productId && validUrls.Contains(img.ImageUrl))
                    .Select(img => img.ImageUrl)
                    .ToListAsync();

                var newUrls = validUrls.Except(existingUrls).ToList();

                if (!newUrls.Any())
                    return BadRequest(new { message = "All provided image URLs already exist for this product" });

                // Create new product images
                var productImages = newUrls.Select(url => new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = url
                }).ToList();

                _context.ProductImages.AddRange(productImages);
                await _context.SaveChangesAsync();

                // Return created images
                var imageResponses = productImages.Select(img => new ProductImageResponse
                {
                    ImageId = img.ImageId,
                    ProductId = img.ProductId ?? 0,
                    ImageUrl = img.ImageUrl,
                    ProductName = product.ProductName ?? "Unknown Product"
                }).ToList();

                return Ok(new { 
                    message = $"Successfully added {newUrls.Count} images. {existingUrls.Count} URLs were already existing.",
                    images = imageResponses 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while bulk uploading images", details = ex.Message });
            }
        }
    }
}
