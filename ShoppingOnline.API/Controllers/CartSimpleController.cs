using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartSimpleController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public CartSimpleController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get current user's cart
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<object>> GetCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    return Ok(new 
                    { 
                        message = "Cart is empty",
                        items = new List<object>(),
                        totalAmount = 0
                    });
                }

                var cartResponse = new
                {
                    CartId = cart.CartId,
                    UserId = cart.UserId,
                    Items = cart.CartItems.Select(ci => new
                    {
                        CartItemId = ci.CartItemId,
                        ProductId = ci.ProductId,
                        ProductName = ci.Product?.ProductName ?? "Unknown Product",
                        ProductPrice = ci.Product?.Price ?? 0,
                        Quantity = ci.Quantity,
                        TotalPrice = (ci.Product?.Price ?? 0) * ci.Quantity,
                        IsInStock = (ci.Product?.StockQuantity ?? 0) > 0
                    }).ToList(),
                    TotalAmount = cart.CartItems.Sum(ci => (ci.Product?.Price ?? 0) * ci.Quantity),
                    TotalItems = cart.CartItems.Sum(ci => ci.Quantity)
                };

                return Ok(cartResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the cart", details = ex.Message });
            }
        }

        /// <summary>
        /// Add item to cart
        /// </summary>
        [HttpPost("add")]
        public async Task<ActionResult<object>> AddToCart([FromBody] AddToCartSimpleRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                // Validate product exists
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { message = "Product not found" });

                if (product.StockQuantity < request.Quantity)
                    return BadRequest(new { message = $"Insufficient stock. Available: {product.StockQuantity}" });

                // Get or create cart
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId.Value);
                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Check if item already exists in cart
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == request.ProductId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += request.Quantity;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity
                    };
                    _context.CartItems.Add(cartItem);
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Item added to cart successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding item to cart", details = ex.Message });
            }
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("remove/{cartItemId}")]
        public async Task<ActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart!.UserId == userId);

                if (cartItem == null)
                    return NotFound(new { message = "Cart item not found" });

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Item removed from cart successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while removing item from cart", details = ex.Message });
            }
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        #endregion
    }

    public class AddToCartSimpleRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
