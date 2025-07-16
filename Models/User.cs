using Microsoft.AspNetCore.Identity;

namespace ECommerceApi.Models
{
    public class User : IdentityUser<int>
    {
        public string Name { get; set; } = null!;
        public string Role { get; set; } = "User";
        public bool IsSuperAdmin { get; set; } = false;
        public bool IsMainSuperAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
    }
}
