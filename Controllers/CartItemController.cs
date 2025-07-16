using ECommerceApi.Data;
using ECommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/cartitems")]
    [Authorize] // ✅ Allows all authenticated users (any role)
    public class CartItemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CartItemController(ApplicationDbContext context) => _context = context;

        [HttpPost("item/add")]
        public async Task<IActionResult> AddCartItem(CartItem item)
        {
            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();
            return Ok("Item added to cart");
        }

        [HttpPut("cart/{cartId}/product/{productId}/update")]
        public async Task<IActionResult> UpdateItem(int cartId, int productId, CartItem updated)
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId);
            if (item == null) return NotFound();

            item.Quantity = updated.Quantity;
            await _context.SaveChangesAsync();
            return Ok("Cart item updated");
        }

        [HttpDelete("cart/{cartId}/product/{productId}/remove")]
        public async Task<IActionResult> RemoveItem(int cartId, int productId)
        {
            var item = await _context.CartItems.FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId);
            if (item == null) return NotFound();

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok("Item removed from cart");
        }
    }
}