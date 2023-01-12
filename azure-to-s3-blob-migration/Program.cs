using System.Text.RegularExpressions;
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

            var keyPrefix = "";
            if (c.Name.StartsWith("imports-dev-"))
            {
                var rg = new Regex(@"\d*$");
                var match = rg.Match(c.Name);
                keyPrefix = match.Value;
            }
            
            var containerClient = blobClient.GetBlobContainerClient(c.Name);
            var blobs = containerClient.GetBlobsAsync();
            await foreach (var b in blobs)
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
        => new ("UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://localhost");
}