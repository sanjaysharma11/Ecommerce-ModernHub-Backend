using ECommerceApi.Data;
using ECommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/carts")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdString = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdString))
                throw new UnauthorizedAccessException("User ID claim not found.");

            return int.Parse(userIdString);
        }

        [HttpGet("my-cart")]
        public async Task<IActionResult> GetMyCart()
        {
            var userId = GetUserId();

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    Items = new List<CartItem>()
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartDto = new CartDto
            {
                UserId = cart.UserId,
                Items = cart.Items.Select(i => new CartItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    Quantity = i.Quantity,
                    Price = i.Product?.Price ?? 0,
                    ImageUrls = i.Product?.Images.Select(img => img.Url).ToList() ?? new List<string>()
                }).ToList()
            };

            return Ok(cartDto);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest("Quantity must be at least 1.");

            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, Items = new List<CartItem>() };
                _context.Carts.Add(cart);
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    Cart = cart
                };
                cart.Items.Add(newItem);
            }

            await _context.SaveChangesAsync();
            return Ok("Item added to cart.");
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request)
        {
            if (request.Quantity < 0)
                return BadRequest("Quantity cannot be negative.");

            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item == null)
                return NotFound("Item not found in cart.");

            if (request.Quantity == 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity;
            }

            await _context.SaveChangesAsync();
            return Ok("Cart updated.");
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveCartItemRequest request)
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (item == null)
                return NotFound("Item not found.");

            cart.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Ok("Item removed from cart.");
        }

        [HttpPost("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found.");

            cart.Items.Clear();
            await _context.SaveChangesAsync();

            return Ok("Cart cleared.");
        }
    }

    public class CartDto
    {
        public int UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }

    public class AddCartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartItemRequest
    {
        public int ProductId { get; set; }
    }
}
