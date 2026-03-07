using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace project_lifecycle.Services
{
    public interface IS3StorageService
    {
        Task<string> UploadHtmlAsync(string key, string htmlContent, string contentType = "text/html");
    }

    public class S3StorageService : IS3StorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3StorageService(IConfiguration configuration)
        {
            var awsSection = configuration.GetSection("AWS");
            _bucketName = awsSection["BucketName"] ?? throw new InvalidOperationException("AWS:BucketName is not configured.");
            var region = awsSection["Region"] ?? "us-east-1";
            var accessKey = awsSection["AccessKey"] ?? string.Empty;
            var secretKey = awsSection["SecretKey"] ?? string.Empty;

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                _s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
            }
            else
            {
                // Falls back to environment variables, IAM role, or credentials file
                _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
            }
        }

        public async Task<string> UploadHtmlAsync(string key, string htmlContent, string contentType = "text/html")
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent));

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request);

            return $"https://{_bucketName}.s3.amazonaws.com/{key}";
        }
    }
}
