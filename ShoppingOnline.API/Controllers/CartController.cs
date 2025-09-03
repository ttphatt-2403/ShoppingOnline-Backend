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
    public class CartController : BaseController
    {
        private readonly ShoppingDbContext _context;

        public CartController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get current user's cart
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return ErrorResponse("User not authenticated", statusCode: 401);

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Variant)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    // Create empty cart for user
                    cart = new Cart
                    {
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartResponse = new
                {
                    cartId = cart.CartId,
                    userId = cart.UserId,
                    items = cart.CartItems.Select(ci => new
                    {
                        cartItemId = ci.CartItemId,
                        productId = ci.ProductId,
                        productName = ci.Product?.ProductName,
                        price = ci.Product?.Price ?? 0,
                        discount = ci.Product?.Discount ?? 0,
                        quantity = ci.Quantity,
                        variantId = ci.VariantId,
                        variant = ci.Variant != null ? new
                        {
                            size = ci.Variant.Size,
                            color = ci.Variant.Color
                        } : null,
                        productImage = ci.Product?.ProductImages?.FirstOrDefault()?.ImageUrl,
                        subtotal = (ci.Product?.Price ?? 0) * ci.Quantity
                    }).ToList(),
                    totalItems = cart.CartItems.Sum(ci => ci.Quantity),
                    totalAmount = cart.CartItems.Sum(ci => (ci.Product?.Price ?? 0) * ci.Quantity),
                    createdAt = cart.CreatedAt
                };

                return Ok(new
                {
                    Success = true,
                    Message = "Cart retrieved successfully",
                    Data = cartResponse,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while retrieving cart", ex.Message, 500);
            }
        }

        /// <summary>
        /// Add item to cart
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return ErrorResponse("User not authenticated", statusCode: 401);

                // Validate product exists
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return ErrorResponse("Product not found", statusCode: 404);

                // Check stock availability
                if (product.StockQuantity < request.Quantity)
                    return ErrorResponse($"Only {product.StockQuantity} items available in stock");

                // Get or create cart
                var cart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId);

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
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && 
                                             ci.ProductId == request.ProductId &&
                                             ci.VariantId == request.ProductVariantId);

                if (existingCartItem != null)
                {
                    // Update quantity
                    existingCartItem.Quantity += request.Quantity;
                    
                    // Check stock again
                    if (product.StockQuantity < existingCartItem.Quantity)
                        return ErrorResponse($"Only {product.StockQuantity} items available in stock");
                }
                else
                {
                    // Add new cart item
                    var cartItem = new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = request.ProductId,
                        VariantId = request.ProductVariantId,
                        Quantity = request.Quantity
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Item added to cart successfully",
                    Data = (object?)null,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while adding item to cart", ex.Message, 500);
            }
        }

        /// <summary>
        /// Update cart item quantity
        /// </summary>
        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateCartItem(int cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return ErrorResponse("User not authenticated", statusCode: 401);

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

                if (cartItem == null)
                    return ErrorResponse("Cart item not found", statusCode: 404);

                // Verify ownership
                if (cartItem.Cart?.UserId != userId)
                    return ErrorResponse("Unauthorized access to cart item", statusCode: 403);

                // Check stock availability
                if (cartItem.Product?.StockQuantity < request.Quantity)
                    return ErrorResponse($"Only {cartItem.Product?.StockQuantity} items available in stock");

                cartItem.Quantity = request.Quantity;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Cart item updated successfully",
                    Data = (object?)null,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while updating cart item", ex.Message, 500);
            }
        }

        /// <summary>
        /// Remove item from cart
        /// </summary>
        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return ErrorResponse("User not authenticated", statusCode: 401);

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

                if (cartItem == null)
                    return ErrorResponse("Cart item not found", statusCode: 404);

                // Verify ownership
                if (cartItem.Cart?.UserId != userId)
                    return ErrorResponse("Unauthorized access to cart item", statusCode: 403);

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Item removed from cart successfully",
                    Data = (object?)null,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while removing item from cart", ex.Message, 500);
            }
        }

        /// <summary>
        /// Clear all items from cart
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return ErrorResponse("User not authenticated", statusCode: 401);

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null && cart.CartItems.Any())
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Cart cleared successfully",
                    Data = (object?)null,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResponse("An error occurred while clearing cart", ex.Message, 500);
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }

    // DTOs for Cart operations
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }
}
