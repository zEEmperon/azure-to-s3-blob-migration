using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs.Models;

namespace azure_to_s3_blob_migration;

public class MigrationS3Client
{
    private readonly IAmazonS3 _s3Client;
    public MigrationS3Client()
    {
        var awsCredentials = new EnvironmentVariablesAWSCredentials();

        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.EUWest1,
        };
        
        _s3Client = new AmazonS3Client(awsCredentials, s3Config);
    }
    
    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        if (!await _s3Client.DoesS3BucketExistAsync(bucketName))
        {
            await _s3Client.PutBucketAsync(bucketName, cancellationToken);
        }
    }
    
    public async Task PutAsync(string bucketName, string key, BlobDownloadResult blob,
        CancellationToken token = default)
    {
        await EnsureBucketExistsAsync(bucketName, token);
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = blob.Content.ToStream(),
            ContentType = blob.Details.ContentType
        };
        foreach (var m in blob.Details.Metadata)
        {
            request.Metadata.Add(m.Key, m.Value);
        }
        await _s3Client.PutObjectAsync(request, token);
    }
    
    public async Task<byte[]> GetAsync(string bucketName, string key, CancellationToken token = default)
    {
        await EnsureBucketExistsAsync(bucketName, token);
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = key
        };
        
        var response = await _s3Client.GetObjectAsync(request, token);
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, token);
        return ms.ToArray();
    }
}