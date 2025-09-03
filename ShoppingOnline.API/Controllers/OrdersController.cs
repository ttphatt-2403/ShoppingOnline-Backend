using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.Constants;
using ShoppingOnline.API.DTOs.Orders;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public OrdersController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all orders for current user (or all orders for Admin/OrderManager)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOrders()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                // Role-based filtering
                if (userRole != RoleConstants.Names.Admin && userRole != RoleConstants.Names.OrderManager)
                {
                    query = query.Where(o => o.UserId == userId.Value);
                }

                var orders = await query
                    .Select(o => new
                    {
                        o.OrderId,
                        o.UserId,
                        UserName = o.User!.Username,
                        UserEmail = o.User.Email,
                        o.OrderDate,
                        o.TotalAmount,
                        o.ShippingAddress,
                        o.PaymentStatus,
                        o.ShippingStatus,
                        Items = o.OrderItems.Select(oi => new
                        {
                            oi.OrderItemId,
                            oi.ProductId,
                            ProductName = oi.Product!.ProductName,
                            oi.PriceAtOrder,
                            oi.Quantity,
                            TotalPrice = oi.PriceAtOrder * oi.Quantity
                        })
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetOrder(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Where(o => o.OrderId == id);

                // Role-based access control
                if (userRole != RoleConstants.Names.Admin && userRole != RoleConstants.Names.OrderManager)
                {
                    query = query.Where(o => o.UserId == userId.Value);
                }

                var order = await query
                    .Select(o => new
                    {
                        o.OrderId,
                        o.UserId,
                        UserName = o.User!.Username,
                        UserEmail = o.User.Email,
                        o.OrderDate,
                        o.TotalAmount,
                        o.ShippingAddress,
                        o.PaymentStatus,
                        o.ShippingStatus,
                        Items = o.OrderItems.Select(oi => new
                        {
                            oi.OrderItemId,
                            oi.ProductId,
                            ProductName = oi.Product!.ProductName,
                            oi.PriceAtOrder,
                            oi.Quantity,
                            TotalPrice = oi.PriceAtOrder * oi.Quantity
                        })
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new { message = $"Order with ID {id} not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the order", details = ex.Message });
            }
        }

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        #endregion
    }
}
