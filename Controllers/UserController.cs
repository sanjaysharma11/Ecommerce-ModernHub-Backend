using ECommerceApi.Data;
using ECommerceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserController(ApplicationDbContext context) => _context = context;

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("user/{id}/update")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updated)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Name = updated.Name;
            user.Role = updated.Role;
            await _context.SaveChangesAsync();

            return Ok("User updated");
        }

        [HttpDelete("user/{id}/delete")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.IsMainSuperAdmin)
                return BadRequest("Cannot delete the main SuperAdmin.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok("User deleted");
        }
    }
}
