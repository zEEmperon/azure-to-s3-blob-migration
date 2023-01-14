using System.Text.RegularExpressions;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Azure.Storage.Blobs;

namespace azure_to_s3_blob_migration;

static class Program
{
    static async Task Main(string[] args)
    {
        var cancellationToken = new CancellationToken();

        var importsBucketName = "imports-bucket";
        var attachmentsBucketName = "attachments-bucket";

        var azureBlobClient = GetBlobServiceClient();
        var s3Client = GetS3Client();

        await EnsureBucketExistsAsync(s3Client, importsBucketName, cancellationToken);
        await EnsureBucketExistsAsync(s3Client, attachmentsBucketName, cancellationToken);
        
        var azureContainers = azureBlobClient.GetBlobContainersAsync();
        await foreach (var c in azureContainers)
        {
            Console.WriteLine("Container: " + c.Name);

            var keyPrefix = "";
            if (c.Name.StartsWith("imports-dev-"))
            {
                var rg = new Regex(@"\d*$");
                var match = rg.Match(c.Name);
                keyPrefix = match.Value;
            }
            
            var azureContainerClient = azureBlobClient.GetBlobContainerClient(c.Name);
            var azureBlobs = azureContainerClient.GetBlobsAsync();
            await foreach (var b in azureBlobs)
            {
                Console.WriteLine("\tKey = " + b.Name);
                if (c.Name.StartsWith("imports-dev-"))
                {
                    Console.WriteLine("\tNew key = " + keyPrefix + '-' + b.Name);
                    Console.WriteLine();
                }

            }
        }
    }

    private static BlobServiceClient GetBlobServiceClient()
    {
        return new BlobServiceClient
            ("UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost");
    }

    private static AmazonS3Client GetS3Client()
    {
        var awsCredentials = new BasicAWSCredentials
            ("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = "http://localhost:9444",
            ForcePathStyle = true,
            UseHttp = true
        };

        return new AmazonS3Client(awsCredentials, s3Config);
    }
    
    private static async Task EnsureBucketExistsAsync(IAmazonS3 s3Client, string bucketName, CancellationToken cancellationToken)
    {
        if (!await s3Client.DoesS3BucketExistAsync(bucketName))
        {
            await s3Client.PutBucketAsync(bucketName, cancellationToken);
        }
    }
}