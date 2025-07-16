using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ECommerceApi.Models
{
    public class ProductCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string? Currency { get; set; }
        public bool IsFeatured { get; set; } = false;
        public IFormFileCollection? Images { get; set; }
        public List<string>? Sizes { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? DiscountPercentage { get; set; }
        public bool GenerateCoupon { get; set; } = false;
        public string? CouponCode { get; set; }
    }
}
