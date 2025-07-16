using ECommerceApi.Data;
using ECommerceApi.DTOs;
using ECommerceApi.Models;
using ECommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApi.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _tokenService;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, TokenService tokenService, EmailService emailService, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _emailService = emailService;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email already in use." });

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "User",
                IsSuperAdmin = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role,
                    joinDate = user.CreatedAt.ToString("dd-MM-yyyy") // Use actual creation date
                }
            });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin(RegisterRequest request)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (currentUser == null || !currentUser.IsSuperAdmin)
                return Forbid("Only SuperAdmin can create new admins.");

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email already in use." });

            var newAdmin = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "Admin",
                IsSuperAdmin = false
            };

            _context.Users.Add(newAdmin);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = newAdmin.Id,
                name = newAdmin.Name,
                email = newAdmin.Email,
                role = newAdmin.Role,
                joinDate = newAdmin.CreatedAt.ToString("dd-MM-yyyy") // Use actual creation date
            });
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("register-superadmin")]
        public async Task<IActionResult> RegisterSuperAdmin(RegisterRequest request)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (currentUser == null || !currentUser.IsSuperAdmin)
                return Forbid("Only an existing SuperAdmin can create a new SuperAdmin.");

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email already in use." });

            var newSuperAdmin = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "SuperAdmin",
                IsSuperAdmin = true,
                IsMainSuperAdmin = false
            };

            _context.Users.Add(newSuperAdmin);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = newSuperAdmin.Id,
                name = newSuperAdmin.Name,
                email = newSuperAdmin.Email,
                role = newSuperAdmin.Role,
                joinDate = newSuperAdmin.CreatedAt.ToString("dd-MM-yyyy") // Use actual creation date
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            if (user.Role == "Admin" || user.Role == "SuperAdmin")
                return Unauthorized(new { message = "Admins cannot log in here." });

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role,
                    joinDate = user.CreatedAt.ToString("dd-MM-yyyy") // Use actual creation date
                }
            });
        }

        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin(LoginRequest request)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid email or password." });

            if (user.Role != "Admin" && user.Role != "SuperAdmin")
                return Unauthorized(new { message = "Only Admin or SuperAdmin can log in here." });

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role,
                    joinDate = user.CreatedAt.ToString("dd-MM-yyyy") // Use actual creation date
                }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || user.Role != "User")
                return Ok(new { message = "If the email exists, a reset link has been sent." });

            var resetToken = GenerateSecureToken();
            user.ResetToken = resetToken;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var resetUrl = $"{_config["Frontend:ResetPasswordUrl"]}?token={resetToken}&email={Uri.EscapeDataString(request.Email)}";
            var message = $@"
                <h2>Password Reset Request</h2>
                <p>You have requested to reset your password. Click the link below to reset your password:</p>
                <p><a href=""{resetUrl}"" style=""background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you did not request this password reset, please ignore this email.</p>
            ";

            try
            {
                await _emailService.SendEmailAsync(request.Email, "Password Reset Request", message, isHtml: true);
            }
            catch (Exception ex)
            {
                // Log the error but don't reveal it to the user
                // _logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
            }

            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || user.Role != "User")
                return BadRequest(new { message = "Invalid reset request. This endpoint is for users only." });

            if (string.IsNullOrEmpty(user.ResetToken) || user.ResetToken != request.Token || user.ResetTokenExpires < DateTime.UtcNow)
                return BadRequest(new { message = "Invalid or expired reset token." });

            // Add password validation
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters long." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpires = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successful." });
        }

        [HttpPost("admin-forgot-password")]
        public async Task<IActionResult> AdminForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || (user.Role != "Admin" && user.Role != "SuperAdmin"))
                return Ok(new { message = "If the email exists, a reset link has been sent." });

            var resetToken = GenerateSecureToken();
            user.ResetToken = resetToken;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var resetUrl = $"{_config["Frontend:AdminResetPasswordUrl"]}?token={resetToken}&email={Uri.EscapeDataString(request.Email)}";
            var message = $@"
                <h2>Admin Password Reset Request</h2>
                <p>You have requested to reset your admin password. Click the link below to reset your password:</p>
                <p><a href=""{resetUrl}"" style=""background-color: #FF6B6B; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;"">Reset Admin Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you did not request this password reset, please ignore this email and contact your system administrator.</p>
            ";

            try
            {
                await _emailService.SendEmailAsync(request.Email, "Admin Password Reset Request", message, isHtml: true);
            }
            catch (Exception ex)
            {
                // Log the error but don't reveal it to the user
                // _logger.LogError(ex, "Failed to send admin password reset email to {Email}", request.Email);
            }

            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("admin-reset-password")]
        public async Task<IActionResult> AdminResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || (user.Role != "Admin" && user.Role != "SuperAdmin"))
                return BadRequest(new { message = "Invalid reset request. This endpoint is for admins only." });

            if (string.IsNullOrEmpty(user.ResetToken) || user.ResetToken != request.Token || user.ResetTokenExpires < DateTime.UtcNow)
                return BadRequest(new { message = "Invalid or expired reset token." });

            // Add password validation
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters long." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpires = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin password reset successful." });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound(new { message = "User not found." });

            return Ok(new
            {
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = user.Role,
                    joinDate = user.CreatedAt.ToString("dd-MM-yyyy") // Use actual creation date
                }
            });
        }

        private string GenerateSecureToken()
        {
            // Generate a more secure token using cryptographically secure random number generator
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}