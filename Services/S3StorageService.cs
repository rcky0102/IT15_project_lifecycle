using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace project_lifecycle.Services
{
    public class S3FileInfo
    {
        public string Key { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public long Size { get; set; }
    }

    public interface IS3StorageService
    {
        Task<string> UploadHtmlAsync(string key, string htmlContent, string contentType = "text/html");
        Task<List<S3FileInfo>> ListFilesAsync(string prefix = "documents/");
        Task<string> GetFileContentAsync(string key);
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

        public async Task<List<S3FileInfo>> ListFilesAsync(string prefix = "documents/")
        {
            var files = new List<S3FileInfo>();
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };

            ListObjectsV2Response response;
            do
            {
                response = await _s3Client.ListObjectsV2Async(request);
                foreach (var obj in response.S3Objects)
                {
                    if (obj.Size == 0) continue; // skip folder markers

                    var fileName = obj.Key;
                    var lastSlash = fileName.LastIndexOf('/');
                    if (lastSlash >= 0) fileName = fileName[(lastSlash + 1)..];

                    files.Add(new S3FileInfo
                    {
                        Key = obj.Key,
                        FileName = fileName,
                        LastModified = obj.LastModified ?? DateTime.MinValue,
                        Size = obj.Size ?? 0
                    });
                }
                request.ContinuationToken = response.NextContinuationToken;
            }
            while (response.IsTruncated == true);

            return files.OrderByDescending(f => f.LastModified).ToList();
        }

        public async Task<string> GetFileContentAsync(string key)
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, key);
            using var reader = new StreamReader(response.ResponseStream);
            return await reader.ReadToEndAsync();
        }
    }
}
