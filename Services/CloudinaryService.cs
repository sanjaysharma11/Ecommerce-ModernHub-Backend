using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ECommerceApi.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var cloudName = config["Cloudinary:CloudName"] ?? throw new ArgumentNullException("Cloudinary:CloudName configuration missing");
            var apiKey = config["Cloudinary:ApiKey"] ?? throw new ArgumentNullException("Cloudinary:ApiKey configuration missing");
            var apiSecret = config["Cloudinary:ApiSecret"] ?? throw new ArgumentNullException("Cloudinary:ApiSecret configuration missing");

            Account account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string?> UploadImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                // Optional: Add image transformations here if needed
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            return uploadResult.SecureUrl?.AbsoluteUri;
        }
    }
}
