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
    public class OrderItemController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public OrderItemController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all order items for an order (User can only see their own orders)
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<object>> GetOrderItems(int orderId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Check if order exists and user has access
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                return NotFound("Order not found");
            }

            // Users can only see their own orders, admins can see all
            if (userRole != "Admin" && order.UserId != userId)
            {
                return Forbid("You can only access your own orders");
            }

            var totalCount = await _context.OrderItems.CountAsync(oi => oi.OrderId == orderId);
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Variant)
                .Where(oi => oi.OrderId == orderId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(oi => new
                {
                    orderItemId = oi.OrderItemId,
                    productId = oi.ProductId,
                    productName = oi.ProductName ?? oi.Product!.ProductName,
                    variantId = oi.VariantId,
                    variantName = oi.VariantName ?? (oi.Variant != null ? $"{oi.Variant.Size} - {oi.Variant.Color}" : null),
                    quantity = oi.Quantity,
                    priceAtOrder = oi.PriceAtOrder,
                    totalPrice = oi.PriceAtOrder * oi.Quantity
                })
                .ToListAsync();

            return Ok(new
            {
                orderId = orderId,
                orderItems = orderItems,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                orderTotal = orderItems.Sum(oi => oi.totalPrice)
            });
        }

        /// <summary>
        /// Get specific order item by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetOrderItem(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Include(oi => oi.Variant)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id);

            if (orderItem == null)
            {
                return NotFound("Order item not found");
            }

            // Users can only see their own order items, admins can see all
            if (userRole != "Admin" && orderItem.Order!.UserId != userId)
            {
                return Forbid("You can only access your own order items");
            }

            return Ok(new
            {
                orderItemId = orderItem.OrderItemId,
                orderId = orderItem.OrderId,
                productId = orderItem.ProductId,
                productName = orderItem.ProductName ?? orderItem.Product?.ProductName,
                variantId = orderItem.VariantId,
                variantName = orderItem.VariantName ?? (orderItem.Variant != null ? $"{orderItem.Variant.Size} - {orderItem.Variant.Color}" : null),
                quantity = orderItem.Quantity,
                priceAtOrder = orderItem.PriceAtOrder,
                totalPrice = orderItem.PriceAtOrder * orderItem.Quantity
            });
        }

        /// <summary>
        /// Get order items by product (Admin only - for analytics)
        /// </summary>
        [HttpGet("product/{productId}")]
        [Authorize(Roles = "Admin,ProductManager")]
        public async Task<ActionResult<object>> GetOrderItemsByProduct(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            var totalCount = await _context.OrderItems.CountAsync(oi => oi.ProductId == productId);
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Variant)
                .Where(oi => oi.ProductId == productId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(oi => new
                {
                    orderItemId = oi.OrderItemId,
                    orderId = oi.OrderId,
                    orderDate = oi.Order!.OrderDate,
                    variantId = oi.VariantId,
                    variantName = oi.VariantName ?? (oi.Variant != null ? $"{oi.Variant.Size} - {oi.Variant.Color}" : null),
                    quantity = oi.Quantity,
                    priceAtOrder = oi.PriceAtOrder,
                    totalPrice = oi.PriceAtOrder * oi.Quantity
                })
                .OrderByDescending(oi => oi.orderDate)
                .ToListAsync();

            var totalQuantitySold = await _context.OrderItems
                .Where(oi => oi.ProductId == productId)
                .SumAsync(oi => oi.Quantity);

            var totalRevenue = await _context.OrderItems
                .Where(oi => oi.ProductId == productId)
                .SumAsync(oi => oi.PriceAtOrder * oi.Quantity);

            return Ok(new
            {
                productId = productId,
                productName = product.ProductName,
                orderItems = orderItems,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                analytics = new
                {
                    totalQuantitySold = totalQuantitySold,
                    totalRevenue = totalRevenue,
                    averageOrderValue = totalCount > 0 ? totalRevenue / totalCount : 0
                }
            });
        }

        /// <summary>
        /// Add order item (Internal use - typically called when creating orders)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,OrderManager")]
        public async Task<ActionResult<object>> CreateOrderItem([FromBody] object request)
        {
            // Parse request body
            var requestData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(request.ToString() ?? "{}");
            var orderId = ((System.Text.Json.JsonElement)requestData).GetProperty("orderId").GetInt32();
            var productId = ((System.Text.Json.JsonElement)requestData).GetProperty("productId").GetInt32();
            var quantity = ((System.Text.Json.JsonElement)requestData).GetProperty("quantity").GetInt32();
            var priceAtOrder = ((System.Text.Json.JsonElement)requestData).GetProperty("priceAtOrder").GetDecimal();

            int? variantId = null;
            if (((System.Text.Json.JsonElement)requestData).TryGetProperty("variantId", out var variantElement))
            {
                variantId = variantElement.GetInt32();
            }

            // Validate order exists
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return BadRequest("Order not found");
            }

            // Validate product exists
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return BadRequest("Product not found");
            }

            // Validate variant if specified
            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = await _context.ProductVariants.FindAsync(variantId);
                if (variant == null || variant.ProductId != productId)
                {
                    return BadRequest("Product variant not found");
                }
            }

            var orderItem = new OrderItem
            {
                OrderId = orderId,
                ProductId = productId,
                VariantId = variantId,
                Quantity = quantity,
                PriceAtOrder = priceAtOrder,
                ProductName = product.ProductName,
                VariantName = variant != null ? $"{variant.Size} - {variant.Color}" : null
            };

            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrderItem), new { id = orderItem.OrderItemId }, new
            {
                orderItemId = orderItem.OrderItemId,
                message = "Order item created successfully"
            });
        }

        /// <summary>
        /// Update order item (Admin only - for corrections)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,OrderManager")]
        public async Task<ActionResult<object>> UpdateOrderItem(int id, [FromBody] object request)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
            {
                return NotFound("Order item not found");
            }

            // Parse request body
            var requestData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(request.ToString() ?? "{}");
            var quantity = ((System.Text.Json.JsonElement)requestData).GetProperty("quantity").GetInt32();
            var priceAtOrder = ((System.Text.Json.JsonElement)requestData).GetProperty("priceAtOrder").GetDecimal();

            orderItem.Quantity = quantity;
            orderItem.PriceAtOrder = priceAtOrder;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                orderItemId = orderItem.OrderItemId,
                quantity = orderItem.Quantity,
                priceAtOrder = orderItem.PriceAtOrder,
                message = "Order item updated successfully"
            });
        }

        /// <summary>
        /// Delete order item (Admin only - for corrections)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,OrderManager")]
        public async Task<IActionResult> DeleteOrderItem(int id)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
            {
                return NotFound("Order item not found");
            }

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order item deleted successfully" });
        }
    }
}
