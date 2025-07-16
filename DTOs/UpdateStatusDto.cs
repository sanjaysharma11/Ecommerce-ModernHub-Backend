namespace ECommerceApi.DTOs
{
    /// <summary>
    /// DTO for updating the status of an order.
    /// </summary>
    public class UpdateStatusDto
    {
        /// <summary>
        /// New status to update the order with (e.g., "Processing", "Shipped", "Delivered").
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
