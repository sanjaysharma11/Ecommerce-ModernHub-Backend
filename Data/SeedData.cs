using ECommerceApi.Models;
using Microsoft.Extensions.Configuration;

namespace ECommerceApi.Data
{
    public static class SeedData
    {
        public static void SeedSuperAdmin(ApplicationDbContext context, IConfiguration config)
        {
            var superAdminConfig = config.GetSection("SuperAdmin");
            var email = superAdminConfig["Email"];
            var name = superAdminConfig["Name"];
            var password = superAdminConfig["Password"];

            if (!context.Users.Any(u => u.Email == email))
            {
                var superAdmin = new User
                {
                    Name = name!,
                    Email = email!,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password!),
                    Role = "SuperAdmin",
                    IsSuperAdmin = true
                };

                context.Users.Add(superAdmin);
                context.SaveChanges();
            }
        }
    }
}
