namespace ECommerceApi.Models
{
    public class Currency
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // e.g., INR, USD
        public string Symbol { get; set; } = string.Empty; // e.g., ₹, $
    }
}
