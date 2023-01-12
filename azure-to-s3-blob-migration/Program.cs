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
            Console.WriteLine(c.Name);
        }
        Console.WriteLine("End");
    }

    private static BlobServiceClient GetBlobServiceClient()
        => new (new Uri("http://127.0.0.1:10000/"));
}