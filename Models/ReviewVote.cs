using System;
using ECommerceApi.Models;

namespace ECommerceApi.Models
{
    public class ReviewVote
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int? UserId { get; set; }
        public string SessionId { get; set; }
        public bool IsHelpful { get; set; }
        public DateTime CreatedAt { get; set; }
        public string VoterIp { get; set; }

        // Navigation properties
        public virtual ProductReview Review { get; set; }
        public virtual User User { get; set; }
    }
}
