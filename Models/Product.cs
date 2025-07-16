    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace ECommerceApi.Models
    {
        public class Product
        {
            public int Id { get; set; }

            [Required]
            public string Name { get; set; } = string.Empty;

            [Required]
            public string Brand { get; set; } = string.Empty;

            [Required]
            public decimal Price { get; set; }

            public int? CategoryId { get; set; }

            [ForeignKey("CategoryId")]
            public Category? Category { get; set; }

            public string? Currency { get; set; }

            public bool IsFeatured { get; set; } = false;

            public string? Description { get; set; }

            public decimal? DiscountPercentage { get; set; }
            public string? CouponCode { get; set; }
            public List<ProductImage> Images { get; set; } = new();
            public List<ProductSize> Sizes { get; set; } = new();
            public List<ProductReview> Reviews { get; set; } = new();
        public decimal OriginalPrice { get; internal set; }
        public decimal? DiscountAmount { get; internal set; }
        public decimal? FinalPrice { get; internal set; }
    }
    }
