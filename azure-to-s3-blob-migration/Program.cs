using System.Text.RegularExpressions;
using Azure.Storage.Blobs;

namespace azure_to_s3_blob_migration;

public enum Environments { Dev, Prod }

public class Config
{
    public string ImportsBucketName { get; set; } = null!;
    public string AttachmentsBucketName { get; set; } = null!;
    public string ImportsBlobContainerName { get; set; } = null!;
    public string AttachmentsBlobContainerName { get; set; } = null!;
}

static class Program
{
    private static Dictionary<Environments, Config> Configs = new Dictionary<Environments, Config>()
    {
        {
            Environments.Dev, new Config()
            {
                ImportsBucketName = "imports-bucket-name-dev",
                AttachmentsBucketName = "attachments-bucket-name-dev",
                ImportsBlobContainerName = "imports-container-name-dev",
                AttachmentsBlobContainerName = "attachments-container-name-dev"
            }
        },
        {
            Environments.Prod, new Config()
            {
                ImportsBucketName = "imports-bucket-name-prod",
                AttachmentsBucketName = "attachments-bucket-name-prod",
                ImportsBlobContainerName = "imports-container-name-prod",
                AttachmentsBlobContainerName = "attachments-container-name-prod"
            }
        }
    };

    private static readonly Config CurrentConfig = Configs[Environments.Prod];

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting script...");
        Console.WriteLine();

        var cancellationToken = new CancellationToken();

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
            if (!IsAppropriateContainer(c.Name))
            {
                continue;
            }

            Console.WriteLine("Container: " + c.Name);
            Console.WriteLine();

            var keyPrefix = "";
            var targetBucketName = CurrentConfig.AttachmentsBucketName;

            if (IsImportsContainer(c.Name))
            {
                var rg = new Regex(@"\d*$");
                var match = rg.Match(c.Name);
                keyPrefix = match.Value;
                targetBucketName = CurrentConfig.ImportsBucketName;
            }

            var azureContainerClient = azureBlobClient.GetBlobContainerClient(c.Name);
            var azureBlobs = azureContainerClient.GetBlobsAsync();
            var i = 0;
            await foreach (var b in azureBlobs)
            {
                var azureSeparateBlobClient = azureContainerClient.GetBlobClient(b.Name);
                var downloadedBlobResponse = await azureSeparateBlobClient.DownloadContentAsync(cancellationToken);

                var key = b.Name;
                var indent = "   ";

                Console.WriteLine($"\t{++i}. Key = " + key);
                if (IsImportsContainer(c.Name))
                {
                    key = $"{keyPrefix}/{b.Name}";
                    Console.WriteLine($"\t{indent}New key = {key}");
                }

                Console.WriteLine($"\t{indent}Uploading to object to Amazon...");
                await s3Client.PutAsync(targetBucketName, key, downloadedBlobResponse.Value, cancellationToken);
                Console.WriteLine($"\t{indent}Successfully uploaded");
                Console.WriteLine();
            }
        }

        Console.WriteLine("End");
    }

    private static bool IsAppropriateContainer(string containerName)
    {
        return containerName == CurrentConfig.AttachmentsBlobContainerName || IsImportsContainer(containerName);
    }

    private static bool IsImportsContainer(string containerName)
    {
        return containerName.StartsWith(CurrentConfig.ImportsBlobContainerName);
    }

    private static BlobServiceClient GetBlobServiceClient()
    {
        return new BlobServiceClient
            ("AzureBlobConnectionString");
    }

    private static MigrationS3Client GetS3Client()
    {
        return new MigrationS3Client();
    }
}