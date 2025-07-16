using ECommerceApi.Data;
using ECommerceApi.Models;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/products")]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CloudinaryService _cloudinaryService;

        public ProductController(ApplicationDbContext context, CloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // POST: /api/v1/products/add
        [HttpPost("add")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> AddProduct([FromForm] ProductCreateRequest request)
        {
            var product = new Product
            {
                Name = request.Name,
                Brand = request.Brand,
                Description = request.Description,
                Price = request.Price,
                CategoryId = request.CategoryId,
                Currency = request.Currency ?? "INR",
                IsFeatured = request.IsFeatured,
                DiscountPercentage = request.DiscountPercentage,
                CouponCode = request.CouponCode,
                Images = new List<ProductImage>(),
                Sizes = new List<ProductSize>()
            };

            if (request.Images?.Count > 0)
            {
                foreach (var file in request.Images)
                {
                    var url = await _cloudinaryService.UploadImageAsync(file);
                    if (!string.IsNullOrEmpty(url))
                    {
                        product.Images.Add(new ProductImage { Url = url });
                    }
                }
            }

            if (request.Sizes != null && request.Sizes.Any())
            {
                foreach (var sizeName in request.Sizes)
                {
                    product.Sizes.Add(new ProductSize { Name = sizeName });
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product added successfully",
                productId = product.Id,
                imageUrls = product.Images.Select(i => i.Url).ToList(),
                sizes = product.Sizes.Select(s => s.Name).ToList()
            });
        }

        // PUT: /api/v1/products/{id}/update
        [HttpPut("{id}/update")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductUpdateRequest request)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            product.Name = request.Name;
            product.Brand = request.Brand;
            product.Description = request.Description;
            product.Price = request.Price;
            product.CategoryId = request.CategoryId;
            product.Currency = request.Currency ?? product.Currency;
            product.IsFeatured = request.IsFeatured;
            product.DiscountPercentage = request.DiscountPercentage;
            product.CouponCode = request.CouponCode;

            if (request.Images?.Count > 0)
            {
                product.Images.Clear();
                foreach (var file in request.Images)
                {
                    var url = await _cloudinaryService.UploadImageAsync(file);
                    if (!string.IsNullOrEmpty(url))
                    {
                        product.Images.Add(new ProductImage { Url = url });
                    }
                }
            }

            if (request.Sizes != null)
            {
                product.Sizes.Clear();
                foreach (var sizeName in request.Sizes)
                {
                    product.Sizes.Add(new ProductSize { Name = sizeName });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Product updated successfully",
                productId = product.Id,
                imageUrls = product.Images.Select(i => i.Url).ToList(),
                sizes = product.Sizes.Select(s => s.Name).ToList()
            });
        }

        // DELETE: /api/v1/products/{id}/delete
        [HttpDelete("{id}/delete")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product deleted successfully" });
        }

        // GET: /api/v1/products/all
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .ToListAsync();

            var response = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Brand,
                p.Description,
                p.Price,
                p.Currency,
                p.CategoryId,
                categoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                p.IsFeatured,
                p.DiscountPercentage,
                p.CouponCode,
                imageUrls = p.Images.Select(img => img.Url).ToList(),
                sizes = p.Sizes.Select(s => s.Name).ToList(),
                averageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                reviewsCount = p.Reviews.Count
            });

            return Ok(response);
        }

        // GET: /api/v1/products/featured
        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeaturedProducts()
        {
            var featuredProducts = await _context.Products
                .Where(p => p.IsFeatured)
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .ToListAsync();

            var response = featuredProducts.Select(p => new
            {
                p.Id,
                p.Name,
                p.Brand,
                p.Description,
                p.Price,
                p.Currency,
                p.CategoryId,
                categoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                p.IsFeatured,
                p.DiscountPercentage,
                p.CouponCode,
                imageUrls = p.Images.Select(img => img.Url).ToList(),
                sizes = p.Sizes.Select(s => s.Name).ToList(),
                averageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                reviewsCount = p.Reviews.Count
            });

            return Ok(response);
        }

        // GET: /api/v1/products/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            var response = new
            {
                product.Id,
                product.Name,
                product.Brand,
                product.Description,
                product.Price,
                product.Currency,
                product.CategoryId,
                categoryName = product.Category != null ? product.Category.Name : "Uncategorized",
                product.IsFeatured,
                product.DiscountPercentage,
                product.CouponCode,
                imageUrls = product.Images.Select(img => img.Url).ToList(),
                sizes = product.Sizes.Select(s => s.Name).ToList(),
                averageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0,
                reviewsCount = product.Reviews.Count
            };

            return Ok(response);
        }

        // ✅ NEW: Voucher (Coupon) Validation Endpoint
        // POST: /api/v1/products/validate-voucher
        [HttpPost("validate-voucher")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateVoucher([FromBody] VoucherValidationRequest request)
        {
            if (string.IsNullOrEmpty(request.VoucherCode) || request.ProductId == 0)
            {
                return BadRequest(new { message = "Voucher code and ProductId are required." });
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.CouponCode == request.VoucherCode);

            if (product == null)
            {
                return NotFound(new { isValid = false, message = "Invalid or expired voucher code." });
            }

            var discount = product.DiscountPercentage ?? 0;
            var discountAmount = product.Price * (discount / 100m);
            var finalPrice = product.Price - discountAmount;

            return Ok(new
            {
                isValid = true,
                originalPrice = product.Price,
                discountPercentage = discount,
                discountAmount,
                finalPrice,
                message = $"Voucher applied! {discount}% off."
            });
        }
    }

    // DTO for voucher validation
    public class VoucherValidationRequest
    {
        public int ProductId { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
    }
}
