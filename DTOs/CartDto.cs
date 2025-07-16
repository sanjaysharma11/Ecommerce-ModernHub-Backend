namespace ECommerceApi.DTOs
{
    public class CartDto
    {
        public int UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
