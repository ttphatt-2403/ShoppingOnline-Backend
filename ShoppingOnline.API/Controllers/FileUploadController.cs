using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoppingOnline.API.Models;
using System.IO;
using System.Threading.Tasks;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly ShoppingDbContext _context;
        private readonly string _imageFolder = "wwwroot/images";

        public FileUploadController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Upload an image file and save its path to database
        /// </summary>
        [HttpPost("product/{productId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadProductImage(int productId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            var fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + System.Guid.NewGuid() + Path.GetExtension(file.FileName);
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), _imageFolder, fileName);
            var relativePath = "/images/" + fileName;

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var productImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = relativePath
            };
            _context.ProductImages.Add(productImage);
            await _context.SaveChangesAsync();

            return Ok(new { imageId = productImage.ImageId, imageUrl = relativePath });
        }
    }
}
