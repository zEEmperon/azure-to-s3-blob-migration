using Azure.Storage.Blobs;

namespace azure_to_s3_blob_migration;

static class Program
{
    static void Main(string[] args)
    {
        var blobClient = GetBlobServiceClient();
        Console.WriteLine(blobClient);
    }

    private static BlobServiceClient GetBlobServiceClient()
        => new (new Uri("http://127.0.0.1:10000/"));
}