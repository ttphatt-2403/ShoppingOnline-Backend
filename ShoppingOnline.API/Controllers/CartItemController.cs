using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartItemController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public CartItemController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all cart items for current user's cart
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetCartItems()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Variant)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return Ok(new { message = "Cart is empty", items = new List<object>() });
            }

            var cartItems = cart.CartItems.Select(ci => new
            {
                cartItemId = ci.CartItemId,
                productId = ci.ProductId,
                productName = ci.Product?.ProductName,
                variantId = ci.VariantId,
                variantName = ci.Variant != null ? $"{ci.Variant.Size} - {ci.Variant.Color}" : null,
                price = ci.Product?.Price ?? 0,
                quantity = ci.Quantity,
                totalPrice = (ci.Product?.Price ?? 0) * ci.Quantity
            }).ToList();

            return Ok(new { 
                cartItems = cartItems,
                totalItems = cartItems.Count,
                totalAmount = cartItems.Sum(item => item.totalPrice)
            });
        }

        /// <summary>
        /// Get specific cart item by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCartItem(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .Include(ci => ci.Variant)
                .FirstOrDefaultAsync(ci => ci.CartItemId == id && ci.Cart!.UserId == userId);

            if (cartItem == null)
            {
                return NotFound("Cart item not found");
            }

            return Ok(new
            {
                cartItemId = cartItem.CartItemId,
                productId = cartItem.ProductId,
                productName = cartItem.Product?.ProductName,
                variantId = cartItem.VariantId,
                variantName = cartItem.Variant != null ? $"{cartItem.Variant.Size} - {cartItem.Variant.Color}" : null,
                price = cartItem.Product?.Price ?? 0,
                quantity = cartItem.Quantity,
                totalPrice = (cartItem.Product?.Price ?? 0) * cartItem.Quantity
            });
        }

        /// <summary>
        /// Add item to cart
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> AddToCart([FromBody] object request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Parse request body
            var requestData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(request.ToString() ?? "{}");
            var productId = ((System.Text.Json.JsonElement)requestData).GetProperty("productId").GetInt32();
            var quantity = ((System.Text.Json.JsonElement)requestData).GetProperty("quantity").GetInt32();
            
            int? variantId = null;
            if (((System.Text.Json.JsonElement)requestData).TryGetProperty("variantId", out var variantElement))
            {
                variantId = variantElement.GetInt32();
            }

            if (quantity <= 0)
            {
                return BadRequest("Quantity must be greater than 0");
            }

            // Check if product exists
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return BadRequest("Product not found");
            }

            // Check variant if specified
            if (variantId.HasValue)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.VariantId == variantId && v.ProductId == productId);
                if (variant == null)
                {
                    return BadRequest("Product variant not found");
                }
            }

            // Get or create cart
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Check if item already exists in cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && 
                                         ci.ProductId == productId && 
                                         ci.VariantId == variantId);

            if (existingCartItem != null)
            {
                // Update quantity
                existingCartItem.Quantity += quantity;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Item quantity updated in cart",
                    cartItemId = existingCartItem.CartItemId,
                    newQuantity = existingCartItem.Quantity
                });
            }
            else
            {
                // Add new item
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCartItem), new { id = cartItem.CartItemId }, new
                {
                    message = "Item added to cart",
                    cartItemId = cartItem.CartItemId
                });
            }
        }

        /// <summary>
        /// Update cart item quantity
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateCartItem(int id, [FromBody] object request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Parse request body
            var requestData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(request.ToString() ?? "{}");
            var quantity = ((System.Text.Json.JsonElement)requestData).GetProperty("quantity").GetInt32();

            if (quantity <= 0)
            {
                return BadRequest("Quantity must be greater than 0");
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == id && ci.Cart!.UserId == userId);

            if (cartItem == null)
            {
                return NotFound("Cart item not found");
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Cart item updated successfully",
                cartItemId = cartItem.CartItemId,
                newQuantity = cartItem.Quantity
            });
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == id && ci.Cart!.UserId == userId);

            if (cartItem == null)
            {
                return NotFound("Cart item not found");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed from cart successfully" });
        }

        /// <summary>
        /// Clear all items from cart
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return Ok(new { message = "Cart is already empty" });
            }

            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart cleared successfully" });
        }
    }
}
