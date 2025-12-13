using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProjectBrain.Domain;

public class Storage
{
    private readonly IConfiguration _configuration;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<Storage> _logger;
    private readonly ISearchIndexService _searchIndexService;

    public Storage(
        IConfiguration configuration,
        BlobServiceClient blobServiceClient,
        ILogger<Storage> logger,
        ISearchIndexService searchIndexService)
    {
        _configuration = configuration;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _searchIndexService = searchIndexService;
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

        // Check if blob exists before proceeding
        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Blob does not exist at location: {Location}", location);
            return false;
        }

        // Get blob information before deleting
        // var blobUrl = blobClient.Uri.ToString();
        var filename = Path.GetFileName(location);

        _logger.LogInformation("Deleting file from blob storage: {Location}, filename: {Filename}", location, filename);

        // Delete documents from search index first
        await _searchIndexService.DeleteDocumentsFromIndexAsync(filename, location);

        // Delete the blob
        var deleted = await blobClient.DeleteIfExistsAsync();

        if (deleted)
        {
            _logger.LogInformation("Successfully deleted file from blob storage: {Location}", location);
        }
        else
        {
            _logger.LogWarning("Failed to delete file from blob storage: {Location}", location);
        }

        return deleted;
    }

    private string getContainerName()
    {
        return _configuration["storage:container"] ?? "resources";
    }

    public async Task<int> ReindexFiles(IResourceService resourceService, string? userId = null)
    {
        _logger.LogInformation("Starting reindex for user {UserId}", userId);

        try
        {
            // Delete all documents for this user from the search index
            await _searchIndexService.DeleteAllDocumentsFromIndexAsync(userId);

            // List all blobs in the user's folder
            var containerName = getContainerName();
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var prefix = $"{(userId is null ? "shared" : userId)}/";

            _logger.LogInformation("Listing blobs with prefix {Prefix} for user {UserId}", prefix, userId);

            var blobs = new List<BlobItem>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobs.Add(blobItem);
            }

            _logger.LogInformation("Found {BlobCount} blobs to reindex for user {UserId}", blobs.Count, userId);

            // Reindex each blob and ensure it exists in database
            int reindexedCount = 0;
            foreach (var blobItem in blobs)
            {
                try
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var filename = Path.GetFileName(blobItem.Name);
                    var blobPath = blobItem.Name;
                    var resourceId = string.Empty;

                    _logger.LogInformation("Reindexing blob: {BlobPath}, filename: {Filename}", blobPath, filename);

                    // If not onboarding data, we need to reindex
                    if (!filename.Equals(Constants.ONBOARDING_DATA_FILENAME))
                    {
                        // Check if resource exists in database by location
                        var resource = (userId is null
                                        ? await resourceService.GetSharedByLocation(blobPath)
                                         : await resourceService.GetForUserByLocation(blobPath, userId!));
                        if (resource == null)
                        {
                            // Get blob properties to get size
                            var blobProperties = await blobClient.GetPropertiesAsync();
                            var blobSize = (int)blobProperties.Value.ContentLength;

                            // Add resource to database
                            var newResource = new Resource()
                            {
                                Id = Guid.NewGuid(),
                                FileName = filename,
                                Location = blobPath,
                                SizeInBytes = blobSize,
                                UserId = userId is null ? string.Empty : userId!,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now,
                                IsShared = userId is null ? true : false
                            };

                            await resourceService.Add(newResource);
                            resource = newResource;
                            _logger.LogInformation("Added resource to database: {BlobPath}", blobPath);
                        }
                        else
                        {
                            _logger.LogInformation("Resource already exists in database: {BlobPath}", blobPath);
                        }

                        resourceId = resource.Id.ToString();

                        // Download blob and reindex
                        await using var stream = await blobClient.OpenReadAsync();
                        await _searchIndexService.ExtractEmbedAndIndexFromStreamAsync(stream, filename, userId, blobPath, resourceId);

                        reindexedCount++;
                        _logger.LogInformation("Successfully reindexed blob: {BlobPath}", blobPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reindexing blob {BlobName} for user {UserId}", blobItem.Name, userId);
                    // Continue with next blob even if one fails
                }
            }

            _logger.LogInformation("Completed reindex for user {UserId}. Reindexed {ReindexedCount} of {TotalCount} files", userId, reindexedCount, blobs.Count);
            return reindexedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reindexing files for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> UploadFile(Stream stream, string filename, string resourceId, string? userId = null, bool skipIndexing = false, Dictionary<string, string>? metadata = null)
    {
        _logger.LogInformation("Starting file upload for user {UserId}, filename {Filename}", userId, filename);

        try
        {
            var containerName = getContainerName();
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Create container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync();

            var blobPath = $"{(userId is null ? "shared" : userId)}/{filename}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation("Uploading file to blob storage at path {BlobPath}", blobPath);

            // Reset stream position if possible
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            await blobClient.UploadAsync(stream, overwrite: true);

            var fileMetadata = metadata ?? new Dictionary<string, string>();
            fileMetadata["userId"] = userId ?? "";
            await blobClient.SetMetadataAsync(metadata);

            // Extract, embed, and index the document
            if (!skipIndexing)
            {
                // Reset stream position again for indexing
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                await _searchIndexService.ExtractEmbedAndIndexFromStreamAsync(stream, filename, userId, blobPath, resourceId);
            }

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for user {UserId}, filename {Filename}", userId, filename);
            throw;
        }
    }

    /// <summary>
    /// Uploads an audio file to blob storage without embedding/indexing.
    /// Used for voice notes and other audio files that don't need to be indexed.
    /// </summary>
    public async Task<string> UploadAudioFile(IFormFile file, string blobPath, string? userId = null, Dictionary<string, string>? metadata = null)
    {
        _logger.LogInformation("Starting audio file upload for user {UserId}, path {BlobPath}", userId, blobPath);

        try
        {
            var containerName = getContainerName();
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            // Create container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation("Uploading audio file to blob storage at path {BlobPath}", blobPath);
            await using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            // Set metadata
            var fileMetadata = metadata ?? new Dictionary<string, string>();
            if (userId != null && !fileMetadata.ContainsKey("userId"))
            {
                fileMetadata["userId"] = userId;
            }
            if (!fileMetadata.ContainsKey("mimeType"))
            {
                fileMetadata["mimeType"] = file.ContentType ?? "audio/m4a";
            }
            await blobClient.SetMetadataAsync(fileMetadata);

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio file for user {UserId}, path {BlobPath}", userId, blobPath);
            throw;
        }
    }

    private async Task ExtractEmbedAndIndexAsync(
        IFormFile file,
        string filename,
        string? userId,
        string blobPath,
        string resourceId)
    {
        await using var stream = file.OpenReadStream();
        await _searchIndexService.ExtractEmbedAndIndexFromStreamAsync(stream, filename, userId, blobPath, resourceId);
    }


    /// <summary>
    /// Generates an Azure Search compliant document ID.
    /// Azure Search IDs can only contain: letters, digits, underscore (_), dash (-), or equal sign (=)
    /// </summary>
    private string GenerateSearchDocumentId(string? userId, string filename, int pageNumber)
    {
        // Sanitize userId - replace invalid characters with underscores
        var sanitizedUserId = SanitizeForSearchId(userId ?? "shared");

        // Sanitize filename - remove extension and sanitize
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filename);
        var sanitizedFilename = SanitizeForSearchId(fileNameWithoutExt);

        // Generate a short unique identifier (first 8 chars of GUID, already hex which is safe)
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Build ID: (userId or "shared")_filename_pageNumber_uniqueId
        // All parts are sanitized, and we use underscores as separators
        return $"{sanitizedUserId}_{sanitizedFilename}_{pageNumber}_{uniqueId}";
    }

    /// <summary>
    /// Sanitizes a string to only contain characters allowed in Azure Search document IDs:
    /// letters, digits, underscore (_), dash (-), or equal sign (=)
    /// Invalid characters are replaced with underscores.
    /// </summary>
    private string SanitizeForSearchId(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "unknown";

        var result = new System.Text.StringBuilder();

        foreach (var c in input)
        {
            // Allow: letters, digits, underscore, dash, equal sign
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '=')
            {
                result.Append(c);
            }
            else
            {
                // Replace invalid characters with underscore
                result.Append('_');
            }
        }

        // Remove consecutive underscores and trim
        var sanitized = result.ToString();
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

        return sanitized.Trim('_');
    }
}