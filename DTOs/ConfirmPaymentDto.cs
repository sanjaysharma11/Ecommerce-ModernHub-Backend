namespace ECommerceApi.DTOs
{
    /// <summary>
    /// DTO for confirming payment after successful Razorpay payment.
    /// </summary>
    public class ConfirmPaymentDto
    {
        /// <summary>
        /// Razorpay payment ID received from frontend after successful payment.
        /// </summary>
        public string PaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Razorpay payment signature used to verify authenticity.
        /// </summary>
        public string Signature { get; set; } = string.Empty;
    }
}
