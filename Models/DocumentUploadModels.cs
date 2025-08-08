using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace NewVivaApi.Models
{
    public enum DocumentType
    {
        PaymentDoc = 1,
        LienReleaseDoc = 2,
        SignUpForm = 3,
        W9 = 4,
        UploadReports = 5,
        SampleForms = 6
    }

    public class S3UploadRequest
    {
        public string Key { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public IFormFile UploadedFile { get; set; } = null!;
    }

    public class S3Upload
    {
        private readonly IAmazonS3 _client;
        private readonly S3UploadRequest _uploadObject;


        public S3Upload(S3UploadRequest uploadObj)
        {
            _uploadObject = uploadObj;
            _client = new AmazonS3Client(RegionEndpoint.USWest2);
        }

        public S3Upload(S3UploadRequest uploadObj, string accessKey, string secretKey)
        {
            _uploadObject = uploadObj;
            _client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.USWest2);
        }

        public async Task Upload()
        {
            try
            {
                using var stream = _uploadObject.UploadedFile.OpenReadStream();

                //Console.WriteLine($"Uploading file to S3: Bucket: {_uploadObject.BucketName}, Key: {_uploadObject.Key}");         

                var putRequest = new PutObjectRequest
                {
                    BucketName = _uploadObject.BucketName,
                    Key = Path.Combine(_uploadObject.FilePath ?? "", _uploadObject.Key).Replace('\\', '/'),
                    ContentType = _uploadObject.UploadedFile.ContentType,
                    InputStream = stream,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                Console.WriteLine($"PutObjectRequest: Bucket: {putRequest.BucketName}, Key: {putRequest.Key}, ContentType: {putRequest.ContentType}");

                PutObjectResponse response = await _client.PutObjectAsync(putRequest);

                Console.WriteLine($"File uploaded successfully to S3: {response.HttpStatusCode}");
                
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"S3 error when uploading file {_uploadObject.Key}: {e.Message}");
                throw new InvalidOperationException($"S3 upload failed: {e.Message}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown error when uploading file {_uploadObject.Key}: {e.Message}");
                throw new InvalidOperationException($"Upload failed: {e.Message}", e);
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}