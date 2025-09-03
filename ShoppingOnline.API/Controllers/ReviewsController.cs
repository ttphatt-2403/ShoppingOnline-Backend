using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingOnline.API.DTOs.Reviews;
using ShoppingOnline.API.Models;
using System.Security.Claims;

namespace ShoppingOnline.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ShoppingDbContext _context;

        public ReviewsController(ShoppingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all reviews for a product
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetProductReviews(int productId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewResponse
                    {
                        ReviewId = r.ReviewId,
                        UserId = r.UserId ?? 0,
                        UserName = r.User != null ? r.User.Username : "Anonymous",
                        ProductId = r.ProductId ?? 0,
                        ProductName = r.Product != null ? r.Product.ProductName : "Unknown Product",
                        Rating = r.Rating ?? 0,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt ?? DateTime.UtcNow
                    })
                    .ToListAsync();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving reviews", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all reviews (with pagination)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReviewResponse
                    {
                        ReviewId = r.ReviewId,
                        UserId = r.UserId ?? 0,
                        UserName = r.User != null ? r.User.Username : "Anonymous",
                        ProductId = r.ProductId ?? 0,
                        ProductName = r.Product != null ? r.Product.ProductName : "Unknown Product",
                        Rating = r.Rating ?? 0,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt ?? DateTime.UtcNow
                    })
                    .ToListAsync();

                var totalCount = await _context.Reviews.CountAsync();

                var result = new
                {
                    reviews = reviews,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving reviews", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new review (authenticated users only)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> CreateReview(CreateReviewRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                // Check if product exists
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Check if user already reviewed this product
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == request.ProductId);

                if (existingReview != null)
                    return BadRequest(new { message = "You have already reviewed this product" });

                // Validate rating
                if (request.Rating < 1 || request.Rating > 5)
                    return BadRequest(new { message = "Rating must be between 1 and 5" });

                var review = new Review
                {
                    UserId = userId.Value,
                    ProductId = request.ProductId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Return the created review with user and product details
                var createdReview = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Where(r => r.ReviewId == review.ReviewId)
                    .Select(r => new ReviewResponse
                    {
                        ReviewId = r.ReviewId,
                        UserId = r.UserId ?? 0,
                        UserName = r.User != null ? r.User.Username : "Anonymous",
                        ProductId = r.ProductId ?? 0,
                        ProductName = r.Product != null ? r.Product.ProductName : "Unknown Product",
                        Rating = r.Rating ?? 0,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt ?? DateTime.UtcNow
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(nameof(GetProductReviews), new { productId = request.ProductId }, createdReview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the review", details = ex.Message });
            }
        }

        /// <summary>
        /// Update a review (only review owner can update)
        /// </summary>
        [HttpPut("{reviewId}")]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> UpdateReview(int reviewId, UpdateReviewRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var review = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

                if (review == null)
                    return NotFound(new { message = "Review not found" });

                // Check if user owns this review
                if (review.UserId != userId.Value)
                    return Forbid("You can only update your own reviews");

                // Validate rating
                if (request.Rating < 1 || request.Rating > 5)
                    return BadRequest(new { message = "Rating must be between 1 and 5" });

                review.Rating = request.Rating;
                review.Comment = request.Comment;

                await _context.SaveChangesAsync();

                var updatedReview = new ReviewResponse
                {
                    ReviewId = review.ReviewId,
                    UserId = review.UserId ?? 0,
                    UserName = review.User != null ? review.User.Username : "Anonymous",
                    ProductId = review.ProductId ?? 0,
                    ProductName = review.Product != null ? review.Product.ProductName : "Unknown Product",
                    Rating = review.Rating ?? 0,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt ?? DateTime.UtcNow
                };

                return Ok(updatedReview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the review", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a review (only review owner can delete)
        /// </summary>
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<ActionResult> DeleteReview(int reviewId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { message = "User not found" });

                var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);

                if (review == null)
                    return NotFound(new { message = "Review not found" });

                // Check if user owns this review
                if (review.UserId != userId.Value)
                    return Forbid("You can only delete your own reviews");

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the review", details = ex.Message });
            }
        }

        /// <summary>
        /// Get product rating statistics
        /// </summary>
        [HttpGet("product/{productId}/stats")]
        public async Task<ActionResult<object>> GetProductRatingStats(int productId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == productId && r.Rating.HasValue)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    return Ok(new
                    {
                        productId = productId,
                        totalReviews = 0,
                        averageRating = 0.0,
                        ratingBreakdown = new Dictionary<int, int>
                        {
                            { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
                        }
                    });
                }

                var totalReviews = reviews.Count;
                var averageRating = reviews.Average(r => r.Rating!.Value);
                var ratingBreakdown = new Dictionary<int, int>();

                for (int i = 1; i <= 5; i++)
                {
                    ratingBreakdown[i] = reviews.Count(r => r.Rating == i);
                }

                var stats = new
                {
                    productId = productId,
                    totalReviews = totalReviews,
                    averageRating = Math.Round(averageRating, 2),
                    ratingBreakdown = ratingBreakdown
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving rating statistics", details = ex.Message });
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
}
