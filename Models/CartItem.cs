namespace ECommerceApi.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        // Foreign Key to Cart
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        // Foreign Key to Product
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Foreign Key to ProductSize (optional)
        public int? ProductSizeId { get; set; }
        public ProductSize? ProductSize { get; set; }

        public int Quantity { get; set; }
    }
}
