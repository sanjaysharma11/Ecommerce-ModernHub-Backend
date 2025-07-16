using ECommerceApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
        public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductSize> ProductSizes => Set<ProductSize>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product images
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product sizes
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Sizes)
                .WithOne(s => s.Product)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product reviews
            modelBuilder.Entity<ProductReview>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId);

            modelBuilder.Entity<ProductReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);

            // Review votes
            modelBuilder.Entity<ReviewVote>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasOne(v => v.Review)
                      .WithMany(r => r.Votes)
                      .HasForeignKey(v => v.ReviewId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.User)
                      .WithMany()
                      .HasForeignKey(v => v.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(v => new { v.ReviewId, v.UserId })
                      .IsUnique();
            });

            // Order & OrderItems
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);
        }
    }
}
