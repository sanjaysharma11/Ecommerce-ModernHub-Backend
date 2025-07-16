using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApi.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string ZipCode { get; set; } = null!;
        public string Country { get; set; } = null!;

        public string Status { get; set; } = "Pending";
        public DateTime OrderDate { get; set; }

        public int Amount { get; set; } // Store amount in paise
        public string Currency { get; set; } = "INR";
        public string? RazorpayOrderId { get; set; }
        public string? PaymentId { get; set; }
        public string? PaymentSignature { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public List<OrderItem> OrderItems { get; set; } = new();

        public string? PaymentMethod { get; set; }
    }
}
