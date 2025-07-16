namespace ECommerceApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // ? Add this navigation property to link products
        public List<Product> Products { get; set; } = new();
    }
}
