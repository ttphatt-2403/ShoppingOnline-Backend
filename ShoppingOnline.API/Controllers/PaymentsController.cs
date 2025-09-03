using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.Payments;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public PaymentsController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all payments (Admin only, with pagination)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Order)
                    .ThenInclude(o => o!.User)
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaymentResponse
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId ?? 0,
                        PaymentMethod = p.PaymentMethod ?? "",
                        Amount = p.Amount ?? 0,
                        PaymentDate = p.PaymentDate ?? DateTime.UtcNow,
                        Status = p.Status ?? "",
                        OrderNumber = p.Order != null ? $"ORD-{p.Order.OrderId}" : "",
                        CustomerName = p.Order != null && p.Order.User != null ? p.Order.User.Username : "Unknown"
                    })
                    .ToListAsync();

                var totalCount = await _context.Payments.CountAsync();

                var result = new
                {
                    payments = payments,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving payments", details = ex.Message });
            }
        }

        /// <summary>
        /// Get payment by ID
        /// </summary>
        [HttpGet("{paymentId}")]
        [Authorize]
        public async Task<ActionResult<PaymentResponse>> GetPayment(int paymentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .ThenInclude(o => o!.User)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                    return NotFound(new { message = "Payment not found" });

                // Users can only view their own payments, admins can view all
                if (userRole != "Admin" && payment.Order?.UserId != userId)
                    return Forbid("You can only view your own payments");

                var paymentResponse = new PaymentResponse
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId ?? 0,
                    PaymentMethod = payment.PaymentMethod ?? "",
                    Amount = payment.Amount ?? 0,
                    PaymentDate = payment.PaymentDate ?? DateTime.UtcNow,
                    Status = payment.Status ?? "",
                    OrderNumber = payment.Order != null ? $"ORD-{payment.Order.OrderId}" : "",
                    CustomerName = payment.Order != null && payment.Order.User != null ? payment.Order.User.Username : "Unknown"
                };

                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the payment", details = ex.Message });
            }
        }

        /// <summary>
        /// Get payments for current user
        /// </summary>
        [HttpGet("my-payments")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<PaymentResponse>>> GetMyPayments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var payments = await _context.Payments
                    .Include(p => p.Order)
                    .ThenInclude(o => o!.User)
                    .Where(p => p.Order!.UserId == userId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new PaymentResponse
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId ?? 0,
                        PaymentMethod = p.PaymentMethod ?? "",
                        Amount = p.Amount ?? 0,
                        PaymentDate = p.PaymentDate ?? DateTime.UtcNow,
                        Status = p.Status ?? "",
                        OrderNumber = p.Order != null ? $"ORD-{p.Order.OrderId}" : "",
                        CustomerName = p.Order != null && p.Order.User != null ? p.Order.User.Username : "Me"
                    })
                    .ToListAsync();

                var totalCount = await _context.Payments
                    .Include(p => p.Order)
                    .CountAsync(p => p.Order!.UserId == userId);

                var result = new
                {
                    payments = payments,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving your payments", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new payment
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PaymentResponse>> CreatePayment(CreatePaymentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                // Check if order exists
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                    return NotFound(new { message = "Order not found" });

                // Users can only create payments for their own orders, admins can create for any order
                if (userRole != "Admin" && order.UserId != userId)
                    return Forbid("You can only create payments for your own orders");

                // Check if payment already exists for this order
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == request.OrderId);

                if (existingPayment != null)
                    return BadRequest(new { message = "Payment already exists for this order" });

                // Validate payment method
                var validPaymentMethods = new[] { "Credit Card", "Debit Card", "PayPal", "Bank Transfer", "Cash" };
                if (!validPaymentMethods.Contains(request.PaymentMethod))
                    return BadRequest(new { message = "Invalid payment method" });

                // Validate status
                var validStatuses = new[] { "Pending", "Processing", "Completed", "Failed", "Cancelled" };
                if (!validStatuses.Contains(request.Status))
                    return BadRequest(new { message = "Invalid payment status" });

                var payment = new Payment
                {
                    OrderId = request.OrderId,
                    PaymentMethod = request.PaymentMethod,
                    Amount = request.Amount,
                    PaymentDate = DateTime.UtcNow,
                    Status = request.Status
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                var paymentResponse = new PaymentResponse
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId ?? 0,
                    PaymentMethod = payment.PaymentMethod ?? "",
                    Amount = payment.Amount ?? 0,
                    PaymentDate = payment.PaymentDate ?? DateTime.UtcNow,
                    Status = payment.Status ?? "",
                    OrderNumber = $"ORD-{order.OrderId}",
                    CustomerName = order.User?.Username ?? "Unknown"
                };

                return CreatedAtAction(nameof(GetPayment), new { paymentId = payment.PaymentId }, paymentResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the payment", details = ex.Message });
            }
        }

        /// <summary>
        /// Update payment status (Admin only)
        /// </summary>
        [HttpPut("{paymentId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentResponse>> UpdatePaymentStatus(int paymentId, PaymentStatusUpdateRequest request)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .ThenInclude(o => o!.User)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                    return NotFound(new { message = "Payment not found" });

                // Validate status
                var validStatuses = new[] { "Pending", "Processing", "Completed", "Failed", "Cancelled" };
                if (!validStatuses.Contains(request.Status))
                    return BadRequest(new { message = "Invalid payment status" });

                payment.Status = request.Status;
                await _context.SaveChangesAsync();

                var paymentResponse = new PaymentResponse
                {
                    PaymentId = payment.PaymentId,
                    OrderId = payment.OrderId ?? 0,
                    PaymentMethod = payment.PaymentMethod ?? "",
                    Amount = payment.Amount ?? 0,
                    PaymentDate = payment.PaymentDate ?? DateTime.UtcNow,
                    Status = payment.Status ?? "",
                    OrderNumber = payment.Order != null ? $"ORD-{payment.Order.OrderId}" : "",
                    CustomerName = payment.Order != null && payment.Order.User != null ? payment.Order.User.Username : "Unknown"
                };

                return Ok(paymentResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating payment status", details = ex.Message });
            }
        }

        /// <summary>
        /// Get payment statistics (Admin only)
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetPaymentStatistics()
        {
            try
            {
                var totalPayments = await _context.Payments.CountAsync();
                var totalAmount = await _context.Payments.SumAsync(p => p.Amount ?? 0);
                
                var paymentsByStatus = await _context.Payments
                    .GroupBy(p => p.Status)
                    .Select(g => new { Status = g.Key ?? "Unknown", Count = g.Count(), Amount = g.Sum(p => p.Amount ?? 0) })
                    .ToListAsync();

                var paymentsByMethod = await _context.Payments
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new { Method = g.Key ?? "Unknown", Count = g.Count(), Amount = g.Sum(p => p.Amount ?? 0) })
                    .ToListAsync();

                var stats = new
                {
                    totalPayments = totalPayments,
                    totalAmount = totalAmount,
                    paymentsByStatus = paymentsByStatus,
                    paymentsByMethod = paymentsByMethod
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving payment statistics", details = ex.Message });
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
