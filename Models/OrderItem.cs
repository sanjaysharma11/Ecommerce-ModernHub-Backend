using ECommerceApi.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string? Size { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
