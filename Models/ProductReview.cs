using System;
using System.Collections.Generic;

namespace ECommerceApi.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product? Product { get; set; }
        public User? User { get; set; }
        public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    }
}
