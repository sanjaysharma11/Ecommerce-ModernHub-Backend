using ECommerceApi.Data;
using ECommerceApi.DTOs;
using ECommerceApi.Models;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RazorpayService _razorpayService;
        private readonly IConfiguration _configuration;

        public OrderController(ApplicationDbContext context, RazorpayService razorpayService, IConfiguration configuration)
        {
            _context = context;
            _razorpayService = razorpayService;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestDto request)
        {
            var userIdClaim = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID claim not found.");

            var userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // Optionally update user's name
            user.Name = $"{request.FirstName} {request.LastName}";
            await _context.SaveChangesAsync();

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    return BadRequest($"Product with ID {item.ProductId} not found.");

                var itemPrice = product.Price * item.Quantity;
                totalAmount += itemPrice;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price,
                    Size = item.Size
                });
            }

            var order = new Order
            {
                UserId = user.Id,
                Status = request.PaymentMethod == "COD" ? "Order Placed" : "Payment Pending",
                OrderDate = DateTime.UtcNow,
                OrderItems = orderItems,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Country = request.Country ?? "India",
                PaymentMethod = request.PaymentMethod ?? "Online"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // If COD, skip Razorpay
            if (request.PaymentMethod == "COD")
            {
                order.Amount = (int)(totalAmount * 100); // Store in paise
                order.Currency = "INR";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    OrderId = order.Id,
                    Amount = order.Amount,
                    Currency = order.Currency,
                    PaymentMethod = "COD",
                    Status = "Order Placed",
                    Message = "Order placed successfully. Payment will be collected on delivery."
                });
            }

            // Online payment flow using Razorpay
            var amountInPaise = (int)(totalAmount * 100);
            var razorpayOrder = _razorpayService.CreateOrder(amountInPaise, "INR", $"order_rcptid_{order.Id}");

            order.RazorpayOrderId = razorpayOrder.Attributes["id"];
            order.Amount = (int)razorpayOrder.Attributes["amount"];
            order.Currency = razorpayOrder.Attributes["currency"];
            await _context.SaveChangesAsync();

            return Ok(new
            {
                OrderId = order.Id,
                RazorpayOrderId = order.RazorpayOrderId,
                Amount = order.Amount,
                Currency = order.Currency,
                PaymentMethod = "Online",
                Status = "Payment Pending"
            });
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserOrders()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID claim not found.");

            var userId = int.Parse(userIdClaim);

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(new
            {
                orders = orders.Select(o => new
                {
                    o.Id,
                    o.Amount,
                    o.Currency,
                    o.Status,
                    o.RazorpayOrderId,
                    o.OrderDate,
                    o.FirstName,
                    o.LastName,
                    o.Email,
                    o.Phone,
                    o.Address,
                    o.City,
                    o.State,
                    o.ZipCode,
                    o.Country,
                    PaymentMethod = o.PaymentMethod ?? "Online",
                    Items = o.OrderItems.Select(i => new
                    {
                        i.ProductId,
                        ProductName = i.Product.Name,
                        i.Quantity,
                        i.Price,
                        i.Size
                    })
                })
            });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(new
            {
                orders = orders.Select(o => new
                {
                    o.Id,
                    o.Amount,
                    o.Currency,
                    o.Status,
                    o.RazorpayOrderId,
                    o.OrderDate,
                    User = new
                    {
                        o.User.Id,
                        o.User.Name,
                        o.User.Email
                    },
                    o.FirstName,
                    o.LastName,
                    o.Email,
                    o.Phone,
                    o.Address,
                    o.City,
                    o.State,
                    o.ZipCode,
                    o.Country,
                    PaymentMethod = o.PaymentMethod ?? "Online",
                    Items = o.OrderItems.Select(i => new
                    {
                        i.ProductId,
                        ProductName = i.Product.Name,
                        i.Quantity,
                        i.Price,
                        i.Size
                    })
                })
            });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{orderId}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            _context.OrderItems.RemoveRange(order.OrderItems);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order deleted successfully by SuperAdmin." });
        }

        [HttpPost("{orderId}/confirm")]
        public async Task<IActionResult> ConfirmPayment(int orderId, [FromBody] ConfirmPaymentDto dto)
        {
            var userIdClaim = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID claim not found.");

            var userId = int.Parse(userIdClaim);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
                return NotFound("Order not found.");

            if (order.PaymentMethod == "COD")
                return BadRequest("COD orders don't require payment confirmation.");

            var secret = _configuration["Razorpay:Secret"];
            if (string.IsNullOrEmpty(secret))
                return StatusCode(500, "Razorpay secret not configured.");

            var payload = order.RazorpayOrderId + "|" + dto.PaymentId;
            var actualSignature = ComputeHash(payload, secret);

            if (actualSignature != dto.Signature)
            {
                order.Status = "Payment Failed";
                await _context.SaveChangesAsync();
                return BadRequest("Invalid payment signature. Order marked as Failed.");
            }

            order.Status = "Payment Paid";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment confirmed and order updated." });
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("{orderId}/status")]
        public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound("Order not found.");

            var validStatuses = new[]
            {
                "Payment Pending", "Payment Paid", "Payment Failed", "Order Placed", "Processing", "Shipped", "InTransit",
                "OutForDelivery", "Delivered", "FailedDelivery", "Returned", "Failed"
            };

            if (!validStatuses.Contains(dto.Status))
                return BadRequest("Invalid status value.");

            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Order status updated to {dto.Status}" });
        }

        private string ComputeHash(string payload, string secret)
        {
            var encoding = new UTF8Encoding();
            byte[] keyBytes = encoding.GetBytes(secret);
            byte[] payloadBytes = encoding.GetBytes(payload);

            using var hmacsha256 = new HMACSHA256(keyBytes);
            byte[] hash = hmacsha256.ComputeHash(payloadBytes);

            return ToHex(hash);
        }

        private string ToHex(byte[] hash)
        {
            var sb = new StringBuilder();
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
