using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using ECommerceApi.Data;
using ECommerceApi.Models;
using ECommerceApi.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/products/{productId}/reviews")]
    public class ProductReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            if (int.TryParse(userIdStr, out int userId))
                return userId;
            return null;
        }

        private string GetAnonymousUserKey(int productId)
        {
            var voterIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"product_{productId}_ip_{voterIp}";
        }

        #region Vote Management APIs

        [HttpPost("~/api/v1/reviews/{reviewId}/vote")]
        [AllowAnonymous]
        public async Task<IActionResult> VoteOnReview(int reviewId, [FromBody] ReviewVoteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return NotFound(new { message = "Review not found." });

            var userId = GetUserId();
            var voterIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var anonymousKey = GetAnonymousUserKey(review.ProductId);

            ReviewVote existingVote = null;

            if (userId.HasValue)
            {
                existingVote = await _context.ReviewVotes
                    .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId);
            }
            else
            {
                existingVote = await _context.ReviewVotes
                    .FirstOrDefaultAsync(v => v.ReviewId == reviewId &&
                                              v.UserId == null &&
                                              v.VoterIp == voterIp);
            }

            if (existingVote != null)
            {
                if (existingVote.IsHelpful == dto.IsHelpful)
                {
                    return BadRequest(new { message = "You have already cast the same vote on this review." });
                }
                else
                {
                    existingVote.IsHelpful = dto.IsHelpful;
                    existingVote.CreatedAt = DateTime.UtcNow;

                    _context.ReviewVotes.Update(existingVote);
                    await _context.SaveChangesAsync();

                    var helpfulCount = await _context.ReviewVotes
                        .CountAsync(v => v.ReviewId == reviewId && v.IsHelpful);
                    var notHelpfulCount = await _context.ReviewVotes
                        .CountAsync(v => v.ReviewId == reviewId && !v.IsHelpful);

                    return Ok(new
                    {
                        message = "Vote updated successfully.",
                        helpfulCount,
                        notHelpfulCount,
                        userVote = new { isHelpful = dto.IsHelpful },
                        action = "updated"
                    });
                }
            }
            else
            {
                var vote = new ReviewVote
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    SessionId = anonymousKey,
                    IsHelpful = dto.IsHelpful,
                    CreatedAt = DateTime.UtcNow,
                    VoterIp = voterIp
                };

                _context.ReviewVotes.Add(vote);
                await _context.SaveChangesAsync();

                var helpfulCount = await _context.ReviewVotes
                    .CountAsync(v => v.ReviewId == reviewId && v.IsHelpful);
                var notHelpfulCount = await _context.ReviewVotes
                    .CountAsync(v => v.ReviewId == reviewId && !v.IsHelpful);

                return Ok(new
                {
                    message = "Vote recorded successfully.",
                    helpfulCount,
                    notHelpfulCount,
                    userVote = new { isHelpful = dto.IsHelpful },
                    action = "created"
                });
            }
        }

        [HttpDelete("~/api/v1/reviews/{reviewId}/vote")]
        [AllowAnonymous]
        public async Task<IActionResult> RemoveVoteOnReview(int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return NotFound(new { message = "Review not found." });

            var userId = GetUserId();
            var voterIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            ReviewVote existingVote = null;

            if (userId.HasValue)
            {
                existingVote = await _context.ReviewVotes
                    .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId);
            }
            else
            {
                existingVote = await _context.ReviewVotes
                    .FirstOrDefaultAsync(v => v.ReviewId == reviewId &&
                                              v.UserId == null &&
                                              v.VoterIp == voterIp);
            }

            if (existingVote == null)
                return NotFound(new { message = "Vote not found." });

            _context.ReviewVotes.Remove(existingVote);
            await _context.SaveChangesAsync();

            var helpfulCount = await _context.ReviewVotes
                .CountAsync(v => v.ReviewId == reviewId && v.IsHelpful);
            var notHelpfulCount = await _context.ReviewVotes
                .CountAsync(v => v.ReviewId == reviewId && !v.IsHelpful);

            return Ok(new
            {
                message = "Vote removed successfully.",
                helpfulCount,
                notHelpfulCount,
                userVote = (object)null
            });
        }

        [HttpGet("~/api/v1/reviews/{reviewId}/votes")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewVotes(int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return NotFound(new { message = "Review not found." });

            var helpfulCount = await _context.ReviewVotes
                .CountAsync(v => v.ReviewId == reviewId && v.IsHelpful);
            var notHelpfulCount = await _context.ReviewVotes
                .CountAsync(v => v.ReviewId == reviewId && !v.IsHelpful);

            var userId = GetUserId();
            var voterIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            ReviewVote userVote = null;

            if (userId.HasValue)
            {
                userVote = await _context.ReviewVotes
                    .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId);
            }
            else
            {
                userVote = await _context.ReviewVotes
                    .FirstOrDefaultAsync(v => v.ReviewId == reviewId &&
                                              v.UserId == null &&
                                              v.VoterIp == voterIp);
            }

            return Ok(new
            {
                reviewId,
                helpfulCount,
                notHelpfulCount,
                userVote = userVote != null ? new { isHelpful = userVote.IsHelpful } : null
            });
        }

        #endregion

        #region Admin Vote Management APIs

        [HttpGet("~/api/v1/admin/votes")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAllVotes()
        {
            var votes = await _context.ReviewVotes
                .Include(v => v.Review)
                    .ThenInclude(r => r.Product)
                .Include(v => v.User)
                .Select(v => new
                {
                    v.Id,
                    v.ReviewId,
                    ReviewComment = v.Review != null ? v.Review.Comment : null,
                    ReviewRating = v.Review != null ? v.Review.Rating : 0,
                    ProductId = v.Review != null ? (int?)v.Review.ProductId : null,
                    ProductName = v.Review != null && v.Review.Product != null ? v.Review.Product.Name : null,
                    v.UserId,
                    UserName = v.User != null ? v.User.Name : "Anonymous",
                    v.SessionId,
                    v.VoterIp,
                    v.IsHelpful,
                    v.CreatedAt
                })
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            var summary = new
            {
                TotalVotes = votes.Count,
                HelpfulVotes = votes.Count(v => v.IsHelpful),
                NotHelpfulVotes = votes.Count(v => !v.IsHelpful),
                RegisteredUserVotes = votes.Count(v => v.UserId.HasValue),
                AnonymousVotes = votes.Count(v => !v.UserId.HasValue)
            };

            return Ok(new
            {
                Summary = summary,
                Votes = votes
            });
        }

        [HttpDelete("~/api/v1/admin/votes")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteAllVotes()
        {
            var votesToDelete = await _context.ReviewVotes.ToListAsync();

            if (!votesToDelete.Any())
                return Ok(new { message = "No votes found to delete.", deletedCount = 0 });

            var deletedCount = votesToDelete.Count;
            _context.ReviewVotes.RemoveRange(votesToDelete);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"All votes deleted successfully.",
                deletedCount = deletedCount,
                deletedAt = DateTime.UtcNow
            });
        }

        [HttpDelete("~/api/v1/admin/votes/review/{reviewId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteVotesByReview(int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return NotFound(new { message = "Review not found." });

            var votesToDelete = await _context.ReviewVotes
                .Where(v => v.ReviewId == reviewId)
                .ToListAsync();

            if (!votesToDelete.Any())
                return Ok(new { message = "No votes found for this review.", deletedCount = 0 });

            var deletedCount = votesToDelete.Count;
            _context.ReviewVotes.RemoveRange(votesToDelete);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"All votes for review {reviewId} deleted successfully.",
                deletedCount = deletedCount,
                reviewId = reviewId,
                deletedAt = DateTime.UtcNow
            });
        }

        [HttpDelete("~/api/v1/admin/votes/product/{productId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteVotesByProduct(int productId)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
                return NotFound(new { message = "Product not found." });

            var reviewIds = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .Select(r => r.Id)
                .ToListAsync();

            if (!reviewIds.Any())
                return Ok(new { message = "No reviews found for this product.", deletedCount = 0 });

            var votesToDelete = await _context.ReviewVotes
                .Where(v => reviewIds.Contains(v.ReviewId))
                .ToListAsync();

            if (!votesToDelete.Any())
                return Ok(new { message = "No votes found for this product's reviews.", deletedCount = 0 });

            var deletedCount = votesToDelete.Count;
            _context.ReviewVotes.RemoveRange(votesToDelete);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"All votes for product {productId} deleted successfully.",
                deletedCount = deletedCount,
                productId = productId,
                deletedAt = DateTime.UtcNow
            });
        }

        #endregion

        #region Review Management APIs

        [HttpGet("~/api/v1/products/reviews/all")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Select(r => new
                {
                    r.Id,
                    r.ProductId,
                    ProductName = r.Product != null ? r.Product.Name : null,
                    r.UserId,
                    UserName = r.User != null ? r.User.Name : null,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    VoteCount = _context.ReviewVotes.Count(v => v.ReviewId == r.Id),
                    HelpfulCount = _context.ReviewVotes.Count(v => v.ReviewId == r.Id && v.IsHelpful),
                    NotHelpfulCount = _context.ReviewVotes.Count(v => v.ReviewId == r.Id && !v.IsHelpful)
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPut("~/api/v1/products/reviews/{reviewId}")]
        [Authorize]
        public async Task<IActionResult> AdminOrUserUpdateReview(int reviewId, [FromBody] ReviewUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return NotFound(new { message = "Review not found." });

            var userId = GetUserId();
            if (userId == null)
                return BadRequest(new { message = "Invalid user ID." });

            var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Admin");
            if (!isAdmin && review.UserId != userId)
                return Forbid();

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            _context.ProductReviews.Update(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review updated successfully." });
        }

        [HttpDelete("~/api/v1/products/reviews/{reviewId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> AdminDeleteReview(int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return NotFound(new { message = "Review not found." });

            var votesToDelete = await _context.ReviewVotes
                .Where(v => v.ReviewId == reviewId)
                .ToListAsync();

            if (votesToDelete.Any())
                _context.ReviewVotes.RemoveRange(votesToDelete);

            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review and associated votes deleted successfully." });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview(int productId, [FromBody] ReviewCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == null)
                return BadRequest(new { message = "Invalid user ID." });

            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
                return NotFound(new { message = "Product not found." });

            var hasOrdered = await _context.Orders
                .Include(o => o.OrderItems)
                .AnyAsync(o => o.UserId == userId && o.OrderItems.Any(i => i.ProductId == productId));

            if (!hasOrdered)
                return BadRequest(new { message = "You can only review products you have ordered." });

            var existingReview = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview != null)
                return BadRequest(new { message = "You have already reviewed this product. Please update your review instead." });

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId.Value,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review created successfully.", reviewId = review.Id });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsForProduct(int productId)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
                return NotFound(new { message = "Product not found." });

            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.Name : null,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    HelpfulCount = _context.ReviewVotes.Count(v => v.ReviewId == r.Id && v.IsHelpful),
                    NotHelpfulCount = _context.ReviewVotes.Count(v => v.ReviewId == r.Id && !v.IsHelpful)
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReview(int productId, int id)
        {
            var review = await _context.ProductReviews
                .Where(r => r.ProductId == productId && r.Id == id)
                .Include(r => r.User)
                .Select(r => new ReviewResponseDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.Name : null,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    HelpfulCount = _context.ReviewVotes.Count(v => v.ReviewId == r.Id && v.IsHelpful),
                    NotHelpfulCount = _context.ReviewVotes.Count(v => v.ReviewId == r.Id && !v.IsHelpful)
                })
                .FirstOrDefaultAsync();

            if (review == null)
                return NotFound(new { message = "Review not found." });

            return Ok(review);
        }

        [HttpGet("summary")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewSummary(int productId)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
                return NotFound(new { message = "Product not found." });

            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .ToListAsync();

            if (!reviews.Any())
                return Ok(new { message = "No reviews found for this product." });

            var averageRating = reviews.Average(r => r.Rating);
            var totalReviews = reviews.Count;

            return Ok(new
            {
                ProductId = productId,
                AverageRating = averageRating,
                TotalReviews = totalReviews
            });
        }

        #endregion

    }
}
