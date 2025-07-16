using Microsoft.Extensions.Configuration;
using Razorpay.Api;
using System.Collections.Generic;

namespace ECommerceApi.Services
{
    public class RazorpayService
    {
        private readonly RazorpayClient _client;

        public RazorpayService(IConfiguration configuration)
        {
            var key = configuration["Razorpay:Key"];
            var secret = configuration["Razorpay:Secret"];
            _client = new RazorpayClient(key, secret);
        }

        public Order CreateOrder(int amount, string currency, string receipt)
        {
            var options = new Dictionary<string, object>
            {
                { "amount", amount },
                { "currency", currency },
                { "receipt", receipt }
            };
            return _client.Order.Create(options);
        }
    }
}
