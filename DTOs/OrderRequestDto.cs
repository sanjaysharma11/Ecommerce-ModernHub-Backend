namespace ECommerceApi.DTOs
{
    /// <summary>
    /// DTO for creating a new order request.
    /// </summary>
    public class OrderRequestDto
    {
        /// <summary>
        /// List of items in the order.
        /// </summary>
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();

        /// <summary>
        /// First name of the customer.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the customer.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the customer.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Phone number of the customer.
        /// </summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Shipping address.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// City for the shipping address.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State for the shipping address.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Zip code for the shipping address.
        /// </summary>
        public string ZipCode { get; set; } = string.Empty;

        /// <summary>
        /// Country for the shipping address (default: India).
        /// </summary>
        public string Country { get; set; } = "India";

        /// <summary>
        /// Payment method chosen by the user (e.g., "COD" or "Online").
        /// </summary>
        public string PaymentMethod { get; set; } = "Online";
    }

    /// <summary>
    /// DTO representing an individual order item.
    /// </summary>
    public class OrderItemDto
    {
        /// <summary>
        /// ID of the product.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Quantity of the product.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Selected size (if applicable).
        /// </summary>
        public string Size { get; set; } = string.Empty;

        /// <summary>
        /// Price of the product (can be left to backend logic).
        /// </summary>
        public int Price { get; internal set; }
    }
}
