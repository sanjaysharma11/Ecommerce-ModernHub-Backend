using ECommerceApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ECommerceApi.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config) => _config = config;

        public string CreateToken(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim("UserId", user.Id.ToString())
            };

            var jwtKey = _config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key configuration is missing.");
            var jwtIssuer = _config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer configuration is missing.");
            var jwtAudience = _config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience configuration is missing.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
