using Azure.Storage.Blobs;

public class Storage
{
    private readonly IConfiguration _configuration;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<Storage> _logger;

    public Storage(
        IConfiguration configuration,
        BlobServiceClient blobServiceClient,
        ILogger<Storage> logger)
    {
        _configuration = configuration;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<Stream?> GetFile(string location)
    {
        var containerName = getContainerName();
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(location);

        if (!await blobClient.ExistsAsync())
            return null;

        var downloadResult = await blobClient.DownloadStreamingAsync();

        return downloadResult.Value.Content;
    }

    public async Task<bool> DeleteFile(string location)
    {
        string containerName = getContainerName();
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(location);

        return await blobClient.DeleteIfExistsAsync();
    }

    private string getContainerName()
    {
        return _configuration["storage:container"] ?? "resources";
    }

    public async Task<string> UploadFile(IFormFile file, string userId, string filename)
    {
        _logger.LogInformation("Starting file upload for user {UserId}, filename {Filename}", userId, filename);

        try
        {
            var containerName = getContainerName();
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            // await containerClient.CreateIfNotExistsAsync();

            var blobPath = $"{userId}/{filename}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation("Uploading file to blob storage at path {BlobPath}", blobPath);
            await using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            var metadata = new Dictionary<string, string>
            {
                { "userId", userId }
            };
            await blobClient.SetMetadataAsync(metadata);

            // This is being executed as a background task every 1 minute. Updat elast file chang etim einstead
            var timestampBlob = containerClient.GetBlobClient("last_filechange_timestamp.txt");
            await timestampBlob.UploadAsync(new BinaryData(DateTimeOffset.UtcNow.ToString("O")), true);

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for user {UserId}, filename {Filename}", userId, filename);
            throw;
        }
    }
}