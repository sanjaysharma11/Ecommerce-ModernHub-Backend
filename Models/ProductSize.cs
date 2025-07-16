namespace ECommerceApi.Models
{
    public class ProductSize
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
