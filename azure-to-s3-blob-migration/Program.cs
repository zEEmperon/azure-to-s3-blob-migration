using System.Text.RegularExpressions;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Azure.Storage.Blobs;

namespace azure_to_s3_blob_migration;

static class Program
{
    static async Task Main(string[] args)
    {
        //AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
        Console.WriteLine("Starting script...");
        Console.WriteLine();
        
        var cancellationToken = new CancellationToken();

        var importsBucketName = "imports-bucket";
        var attachmentsBucketName = "attachments-bucket";

        Console.WriteLine("Creating Azure and S3 Clients...");
        var azureBlobClient = GetBlobServiceClient();
        var s3Client = GetS3Client();
        Console.WriteLine("Clients have been created successfully");
        Console.WriteLine();
        
        Console.WriteLine("Getting Azure containers...");
        var azureContainers = azureBlobClient.GetBlobContainersAsync();
        Console.WriteLine("Containers have been retrieved");
        Console.WriteLine("Starting traversing containers...");
        Console.WriteLine();
        
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
                var azureSeparateBlobClient = azureContainerClient.GetBlobClient(b.Name);
                //var downloadedBlobResponse = await azureSeparateBlobClient.DownloadAsync(cancellationToken);

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

    private static MigrationS3Client GetS3Client()
    {
        return new MigrationS3Client();
    }
}