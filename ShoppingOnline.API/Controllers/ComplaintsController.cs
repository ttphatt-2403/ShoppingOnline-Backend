using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.Complaints;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplaintsController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public ComplaintsController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all complaints (Admin only, with pagination)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ComplaintResponse>>> GetAllComplaints([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.Complaints
                    .Include(c => c.Order)
                    .Include(c => c.User)
                    .AsQueryable();

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                var complaints = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new ComplaintResponse
                    {
                        ComplaintId = c.ComplaintId,
                        OrderId = c.OrderId,
                        UserId = c.UserId ?? 0,
                        Description = c.Description ?? "",
                        Status = c.Status ?? "",
                        CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                        OrderNumber = c.Order != null ? $"ORD-{c.Order.OrderId}" : null,
                        UserName = c.User != null ? c.User.Username : "Unknown User"
                    })
                    .ToListAsync();

                var totalCount = await query.CountAsync();

                var result = new
                {
                    complaints = complaints,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving complaints", details = ex.Message });
            }
        }

        /// <summary>
        /// Get complaint by ID
        /// </summary>
        [HttpGet("{complaintId}")]
        public async Task<ActionResult<ComplaintResponse>> GetComplaint(int complaintId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                var complaint = await _context.Complaints
                    .Include(c => c.Order)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);

                if (complaint == null)
                    return NotFound(new { message = "Complaint not found" });

                // Users can only view their own complaints, admins can view all
                if (userRole != "Admin" && complaint.UserId != userId)
                    return Forbid("You can only view your own complaints");

                var complaintResponse = new ComplaintResponse
                {
                    ComplaintId = complaint.ComplaintId,
                    OrderId = complaint.OrderId,
                    UserId = complaint.UserId ?? 0,
                    Description = complaint.Description ?? "",
                    Status = complaint.Status ?? "",
                    CreatedAt = complaint.CreatedAt ?? DateTime.UtcNow,
                    OrderNumber = complaint.Order != null ? $"ORD-{complaint.Order.OrderId}" : null,
                    UserName = complaint.User?.Username ?? "Unknown User"
                };

                return Ok(complaintResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the complaint", details = ex.Message });
            }
        }

        /// <summary>
        /// Get complaints for current user
        /// </summary>
        [HttpGet("my-complaints")]
        public async Task<ActionResult<IEnumerable<ComplaintResponse>>> GetMyComplaints([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var complaints = await _context.Complaints
                    .Include(c => c.Order)
                    .Include(c => c.User)
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new ComplaintResponse
                    {
                        ComplaintId = c.ComplaintId,
                        OrderId = c.OrderId,
                        UserId = c.UserId ?? 0,
                        Description = c.Description ?? "",
                        Status = c.Status ?? "",
                        CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                        OrderNumber = c.Order != null ? $"ORD-{c.Order.OrderId}" : null,
                        UserName = "Me"
                    })
                    .ToListAsync();

                var totalCount = await _context.Complaints.CountAsync(c => c.UserId == userId);

                var result = new
                {
                    complaints = complaints,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving your complaints", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new complaint
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ComplaintResponse>> CreateComplaint(CreateComplaintRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                // Validate description
                if (string.IsNullOrWhiteSpace(request.Description))
                    return BadRequest(new { message = "Description is required" });

                // Check if order exists and belongs to user (if order is specified)
                Order? order = null;
                if (request.OrderId.HasValue)
                {
                    order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.OrderId == request.OrderId.Value);

                    if (order == null)
                        return NotFound(new { message = "Order not found" });

                    if (order.UserId != userId)
                        return Forbid("You can only create complaints for your own orders");
                }

                var complaint = new Complaint
                {
                    OrderId = request.OrderId,
                    UserId = userId.Value,
                    Description = request.Description,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Complaints.Add(complaint);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId.Value);

                var complaintResponse = new ComplaintResponse
                {
                    ComplaintId = complaint.ComplaintId,
                    OrderId = complaint.OrderId,
                    UserId = complaint.UserId ?? 0,
                    Description = complaint.Description ?? "",
                    Status = complaint.Status ?? "",
                    CreatedAt = complaint.CreatedAt ?? DateTime.UtcNow,
                    OrderNumber = order != null ? $"ORD-{order.OrderId}" : null,
                    UserName = user?.Username ?? "Unknown User"
                };

                return CreatedAtAction(nameof(GetComplaint), new { complaintId = complaint.ComplaintId }, complaintResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the complaint", details = ex.Message });
            }
        }

        /// <summary>
        /// Update complaint status (Admin only)
        /// </summary>
        [HttpPut("{complaintId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ComplaintResponse>> UpdateComplaintStatus(int complaintId, UpdateComplaintStatusRequest request)
        {
            try
            {
                var complaint = await _context.Complaints
                    .Include(c => c.Order)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);

                if (complaint == null)
                    return NotFound(new { message = "Complaint not found" });

                // Validate status
                var validStatuses = new[] { "Pending", "In Progress", "Resolved", "Rejected", "Closed" };
                if (!validStatuses.Contains(request.Status))
                    return BadRequest(new { message = "Invalid complaint status" });

                complaint.Status = request.Status;
                await _context.SaveChangesAsync();

                var complaintResponse = new ComplaintResponse
                {
                    ComplaintId = complaint.ComplaintId,
                    OrderId = complaint.OrderId,
                    UserId = complaint.UserId ?? 0,
                    Description = complaint.Description ?? "",
                    Status = complaint.Status ?? "",
                    CreatedAt = complaint.CreatedAt ?? DateTime.UtcNow,
                    OrderNumber = complaint.Order != null ? $"ORD-{complaint.Order.OrderId}" : null,
                    UserName = complaint.User?.Username ?? "Unknown User"
                };

                return Ok(complaintResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating complaint status", details = ex.Message });
            }
        }

        /// <summary>
        /// Get complaint statistics (Admin only)
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetComplaintStatistics()
        {
            try
            {
                var totalComplaints = await _context.Complaints.CountAsync();
                
                var complaintsByStatus = await _context.Complaints
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key ?? "Unknown", Count = g.Count() })
                    .ToListAsync();

                var recentComplaints = await _context.Complaints
                    .Where(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                var resolvedComplaints = await _context.Complaints
                    .CountAsync(c => c.Status == "Resolved");

                var pendingComplaints = await _context.Complaints
                    .CountAsync(c => c.Status == "Pending");

                var stats = new
                {
                    totalComplaints = totalComplaints,
                    recentComplaints = recentComplaints,
                    resolvedComplaints = resolvedComplaints,
                    pendingComplaints = pendingComplaints,
                    resolutionRate = totalComplaints > 0 ? Math.Round((double)resolvedComplaints / totalComplaints * 100, 2) : 0,
                    complaintsByStatus = complaintsByStatus
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving complaint statistics", details = ex.Message });
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
