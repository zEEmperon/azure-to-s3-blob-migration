using Azure.Storage.Blobs;

namespace azure_to_s3_blob_migration;

static class Program
{
    static async Task Main(string[] args)
    {
        var blobClient = GetBlobServiceClient();
        var containers = blobClient.GetBlobContainersAsync();
        await foreach (var c in containers)
        {
            Console.WriteLine("Container: " + c.Name);
            var containerClient = blobClient.GetBlobContainerClient(c.Name);
            var blobs = containerClient.GetBlobsAsync();
            await foreach (var b in blobs)
            {
                Console.WriteLine("\t" + b.Name);
            }
        }
    }

    private static BlobServiceClient GetBlobServiceClient()
        => new ("UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost");
}