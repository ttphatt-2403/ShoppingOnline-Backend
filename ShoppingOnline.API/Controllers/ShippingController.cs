using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.Shipping;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public ShippingController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all shipping records (Admin only, with pagination)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ShippingResponse>>> GetShippingRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var shippingRecords = await _context.Shippings
                    .Include(s => s.Order)
                    .ThenInclude(o => o!.User)
                    .Include(s => s.Shipper)
                    .OrderByDescending(s => s.ShippingDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new ShippingResponse
                    {
                        ShippingId = s.ShippingId,
                        OrderId = s.OrderId ?? 0,
                        ShipperId = s.ShipperId,
                        ShippingAddress = s.ShippingAddress ?? "",
                        ShippingDate = s.ShippingDate,
                        DeliveryDate = s.DeliveryDate,
                        Status = s.Status ?? "",
                        OrderNumber = s.Order != null ? $"ORD-{s.Order.OrderId}" : "",
                        CustomerName = s.Order != null && s.Order.User != null ? s.Order.User.Username : "Unknown",
                        ShipperName = s.Shipper != null ? s.Shipper.Username : "Unassigned"
                    })
                    .ToListAsync();

                var totalCount = await _context.Shippings.CountAsync();

                var result = new
                {
                    shippingRecords = shippingRecords,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving shipping records", details = ex.Message });
            }
        }

        /// <summary>
        /// Get shipping record by ID
        /// </summary>
        [HttpGet("{shippingId}")]
        [Authorize]
        public async Task<ActionResult<ShippingResponse>> GetShipping(int shippingId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var shipping = await _context.Shippings
                    .Include(s => s.Order)
                    .ThenInclude(o => o!.User)
                    .Include(s => s.Shipper)
                    .FirstOrDefaultAsync(s => s.ShippingId == shippingId);

                if (shipping == null)
                    return NotFound(new { message = "Shipping record not found" });

                // Users can only view their own shipping, shippers can view assigned shipments, admins can view all
                if (userRole != "Admin" && shipping.Order?.UserId != userId && shipping.ShipperId != userId)
                    return Forbid("You can only view your own shipping records");

                var shippingResponse = new ShippingResponse
                {
                    ShippingId = shipping.ShippingId,
                    OrderId = shipping.OrderId ?? 0,
                    ShipperId = shipping.ShipperId,
                    ShippingAddress = shipping.ShippingAddress ?? "",
                    ShippingDate = shipping.ShippingDate,
                    DeliveryDate = shipping.DeliveryDate,
                    Status = shipping.Status ?? "",
                    OrderNumber = shipping.Order != null ? $"ORD-{shipping.Order.OrderId}" : "",
                    CustomerName = shipping.Order != null && shipping.Order.User != null ? shipping.Order.User.Username : "Unknown",
                    ShipperName = shipping.Shipper != null ? shipping.Shipper.Username : "Unassigned"
                };

                return Ok(shippingResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the shipping record", details = ex.Message });
            }
        }

        /// <summary>
        /// Get shipping records for current user's orders
        /// </summary>
        [HttpGet("my-shipments")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ShippingResponse>>> GetMyShipments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var shippingRecords = await _context.Shippings
                    .Include(s => s.Order)
                    .ThenInclude(o => o!.User)
                    .Include(s => s.Shipper)
                    .Where(s => s.Order!.UserId == userId)
                    .OrderByDescending(s => s.ShippingDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new ShippingResponse
                    {
                        ShippingId = s.ShippingId,
                        OrderId = s.OrderId ?? 0,
                        ShipperId = s.ShipperId,
                        ShippingAddress = s.ShippingAddress ?? "",
                        ShippingDate = s.ShippingDate,
                        DeliveryDate = s.DeliveryDate,
                        Status = s.Status ?? "",
                        OrderNumber = s.Order != null ? $"ORD-{s.Order.OrderId}" : "",
                        CustomerName = "Me",
                        ShipperName = s.Shipper != null ? s.Shipper.Username : "Unassigned"
                    })
                    .ToListAsync();

                var totalCount = await _context.Shippings
                    .Include(s => s.Order)
                    .CountAsync(s => s.Order!.UserId == userId);

                var result = new
                {
                    shippingRecords = shippingRecords,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving your shipments", details = ex.Message });
            }
        }

        /// <summary>
        /// Get assigned shipments for shipper
        /// </summary>
        [HttpGet("my-assignments")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ShippingResponse>>> GetMyAssignments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var shippingRecords = await _context.Shippings
                    .Include(s => s.Order)
                    .ThenInclude(o => o!.User)
                    .Include(s => s.Shipper)
                    .Where(s => s.ShipperId == userId)
                    .OrderByDescending(s => s.ShippingDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new ShippingResponse
                    {
                        ShippingId = s.ShippingId,
                        OrderId = s.OrderId ?? 0,
                        ShipperId = s.ShipperId,
                        ShippingAddress = s.ShippingAddress ?? "",
                        ShippingDate = s.ShippingDate,
                        DeliveryDate = s.DeliveryDate,
                        Status = s.Status ?? "",
                        OrderNumber = s.Order != null ? $"ORD-{s.Order.OrderId}" : "",
                        CustomerName = s.Order != null && s.Order.User != null ? s.Order.User.Username : "Unknown",
                        ShipperName = "Me"
                    })
                    .ToListAsync();

                var totalCount = await _context.Shippings.CountAsync(s => s.ShipperId == userId);

                var result = new
                {
                    shippingRecords = shippingRecords,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving your assignments", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new shipping record (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShippingResponse>> CreateShipping(CreateShippingRequest request)
        {
            try
            {
                // Check if order exists
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                    return NotFound(new { message = "Order not found" });

                // Check if shipping record already exists for this order
                var existingShipping = await _context.Shippings
                    .FirstOrDefaultAsync(s => s.OrderId == request.OrderId);

                if (existingShipping != null)
                    return BadRequest(new { message = "Shipping record already exists for this order" });

                // Validate shipper if provided
                User? shipper = null;
                if (request.ShipperId.HasValue)
                {
                    shipper = await _context.Users.FindAsync(request.ShipperId.Value);
                    if (shipper == null)
                        return BadRequest(new { message = "Shipper not found" });
                }

                // Validate status
                var validStatuses = new[] { "Preparing", "Shipped", "In Transit", "Out for Delivery", "Delivered", "Failed", "Returned" };
                if (!validStatuses.Contains(request.Status))
                    return BadRequest(new { message = "Invalid shipping status" });

                var shipping = new Shipping
                {
                    OrderId = request.OrderId,
                    ShipperId = request.ShipperId,
                    ShippingAddress = request.ShippingAddress,
                    ShippingDate = request.ShippingDate ?? DateTime.UtcNow,
                    Status = request.Status
                };

                _context.Shippings.Add(shipping);
                await _context.SaveChangesAsync();

                var shippingResponse = new ShippingResponse
                {
                    ShippingId = shipping.ShippingId,
                    OrderId = shipping.OrderId ?? 0,
                    ShipperId = shipping.ShipperId,
                    ShippingAddress = shipping.ShippingAddress ?? "",
                    ShippingDate = shipping.ShippingDate,
                    DeliveryDate = shipping.DeliveryDate,
                    Status = shipping.Status ?? "",
                    OrderNumber = $"ORD-{order.OrderId}",
                    CustomerName = order.User?.Username ?? "Unknown",
                    ShipperName = shipper?.Username ?? "Unassigned"
                };

                return CreatedAtAction(nameof(GetShipping), new { shippingId = shipping.ShippingId }, shippingResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the shipping record", details = ex.Message });
            }
        }

        /// <summary>
        /// Update shipping status
        /// </summary>
        [HttpPut("{shippingId}/status")]
        [Authorize]
        public async Task<ActionResult<ShippingResponse>> UpdateShippingStatus(int shippingId, ShippingStatusUpdateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var shipping = await _context.Shippings
                    .Include(s => s.Order)
                    .ThenInclude(o => o!.User)
                    .Include(s => s.Shipper)
                    .FirstOrDefaultAsync(s => s.ShippingId == shippingId);

                if (shipping == null)
                    return NotFound(new { message = "Shipping record not found" });

                // Only assigned shipper or admin can update status
                if (userRole != "Admin" && shipping.ShipperId != userId)
                    return Forbid("You can only update shipping status for your assigned shipments");

                // Validate status
                var validStatuses = new[] { "Preparing", "Shipped", "In Transit", "Out for Delivery", "Delivered", "Failed", "Returned" };
                if (!validStatuses.Contains(request.Status))
                    return BadRequest(new { message = "Invalid shipping status" });

                shipping.Status = request.Status;
                if (request.DeliveryDate.HasValue)
                    shipping.DeliveryDate = request.DeliveryDate;

                // Set delivery date automatically when status is "Delivered"
                if (request.Status == "Delivered" && !shipping.DeliveryDate.HasValue)
                    shipping.DeliveryDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var shippingResponse = new ShippingResponse
                {
                    ShippingId = shipping.ShippingId,
                    OrderId = shipping.OrderId ?? 0,
                    ShipperId = shipping.ShipperId,
                    ShippingAddress = shipping.ShippingAddress ?? "",
                    ShippingDate = shipping.ShippingDate,
                    DeliveryDate = shipping.DeliveryDate,
                    Status = shipping.Status ?? "",
                    OrderNumber = shipping.Order != null ? $"ORD-{shipping.Order.OrderId}" : "",
                    CustomerName = shipping.Order != null && shipping.Order.User != null ? shipping.Order.User.Username : "Unknown",
                    ShipperName = shipping.Shipper != null ? shipping.Shipper.Username : "Unassigned"
                };

                return Ok(shippingResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating shipping status", details = ex.Message });
            }
        }

        /// <summary>
        /// Assign shipper to shipping record (Admin only)
        /// </summary>
        [HttpPut("{shippingId}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShippingResponse>> AssignShipper(int shippingId, [FromBody] int shipperId)
        {
            try
            {
                var shipping = await _context.Shippings
                    .Include(s => s.Order)
                    .ThenInclude(o => o!.User)
                    .Include(s => s.Shipper)
                    .FirstOrDefaultAsync(s => s.ShippingId == shippingId);

                if (shipping == null)
                    return NotFound(new { message = "Shipping record not found" });

                // Validate shipper
                var shipper = await _context.Users.FindAsync(shipperId);
                if (shipper == null)
                    return BadRequest(new { message = "Shipper not found" });

                shipping.ShipperId = shipperId;
                await _context.SaveChangesAsync();

                var shippingResponse = new ShippingResponse
                {
                    ShippingId = shipping.ShippingId,
                    OrderId = shipping.OrderId ?? 0,
                    ShipperId = shipping.ShipperId,
                    ShippingAddress = shipping.ShippingAddress ?? "",
                    ShippingDate = shipping.ShippingDate,
                    DeliveryDate = shipping.DeliveryDate,
                    Status = shipping.Status ?? "",
                    OrderNumber = shipping.Order != null ? $"ORD-{shipping.Order.OrderId}" : "",
                    CustomerName = shipping.Order != null && shipping.Order.User != null ? shipping.Order.User.Username : "Unknown",
                    ShipperName = shipper.Username ?? "Unknown"
                };

                return Ok(shippingResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while assigning shipper", details = ex.Message });
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
