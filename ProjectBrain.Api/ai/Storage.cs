using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ProjectBrain.Domain;

public enum FileOwnership
{
    Shared,
    User,
    Coach
}

public enum StorageType
{
    Resources,
    Journal,
    VoiceNotes,
    CoachMessages,
    Onboarding
}

public class StorageOptions
{
    public string UserId { get; set; } = string.Empty;
    public FileOwnership FileOwnership { get; set; } = FileOwnership.User;
    public StorageType StorageType { get; set; } = StorageType.Resources;
    public string ParentFolder { get; set; } = string.Empty;
}

public class StorageUploadOptions : StorageOptions
{
    public string ResourceId { get; set; } = string.Empty;
    public bool SkipIndexing { get; set; } = false;
    public Dictionary<string, string>? Metadata { get; set; } = null;
}

public class Storage
{
    private readonly IConfiguration _configuration;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<Storage> _logger;
    private readonly ISearchIndexService _searchIndexService;

    public const string CONTAINER_NAME = "resources";
    public const string RESOURCES_FOLDER = "resources";
    public const string SHARED_FOLDER = "_shared";
    public const string COACH_MESSAGES_FOLDER = "coach-messages";
    public const string JOURNAL_FOLDER = "journal";
    public const string VOICE_NOTES_FOLDER = "voice-notes";
    public const string ONBOARDING_FOLDER = "onboarding";

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

    public async Task<Stream?> GetFile(string name, StorageOptions options)
    {
        var containerClient = await getContainerClient();

        var location = determineLocation(name, options);

        var blobClient = containerClient.GetBlobClient(location);

        if (!await blobClient.ExistsAsync())
            return null;

        var downloadResult = await blobClient.DownloadStreamingAsync();

        return downloadResult.Value.Content;
    }

    public async Task<string> UploadFile(Stream stream, string name, StorageUploadOptions options)
    {
        _logger.LogInformation("Starting file upload for user {UserId}, filename {Filename}", options.UserId, name);

        try
        {
            var containerClient = await getContainerClient(true);

            //var blobPath = $"{(parentFolder is null ? string.Empty : parentFolder + "/") + (userId is null ? "shared" : userId)}/{filename}";
            var blobPath = determineLocation(name, options);
            var blobClient = containerClient.GetBlobClient(blobPath);

            _logger.LogInformation("Uploading file to blob storage at path {BlobPath}", blobPath);

            // Reset stream position if possible
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            await blobClient.UploadAsync(stream, overwrite: true);

            var fileMetadata = options.Metadata ?? new Dictionary<string, string>();
            fileMetadata["userId"] = options.UserId ?? "";
            await blobClient.SetMetadataAsync(fileMetadata);

            // Extract, embed, and index the document
            if (!options.SkipIndexing)
            {
                // Reset stream position again for indexing
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }
                await _searchIndexService.ExtractEmbedAndIndexFromStreamAsync(stream, name, options.UserId, blobPath, options.ResourceId ?? name);
            }

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for user {UserId}, filename {Filename}", options.UserId, name);
            throw;
        }
    }

    public async Task<bool> DeleteFile(string name, StorageOptions options)
    {
        var containerClient = await getContainerClient();

        var location = determineLocation(name, options);

        var blobClient = containerClient.GetBlobClient(location);

        // Check if blob exists before proceeding
        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Blob does not exist at location: {Location}", location);
            return false;
        }

        _logger.LogInformation("Deleting file from blob storage: {Location}, filename: {Filename}", location, name);

        // Delete documents from search index first
        await _searchIndexService.DeleteDocumentsFromIndexAsync(name, location);

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

    public string determineLocation(string name, StorageOptions options)
    {
        if (options.FileOwnership == FileOwnership.Shared)
            return SHARED_FOLDER;

        if (options.FileOwnership == FileOwnership.User && string.IsNullOrEmpty(options.UserId))
            throw new Exception("User ID is required for user files");

        List<string> locationParts =
        [
            cleanseUserId(options.UserId!),
            options.StorageType switch
            {
                StorageType.Resources => RESOURCES_FOLDER,
                StorageType.Journal => JOURNAL_FOLDER,
                StorageType.VoiceNotes => VOICE_NOTES_FOLDER,
                StorageType.CoachMessages => COACH_MESSAGES_FOLDER,
                StorageType.Onboarding => ONBOARDING_FOLDER,
                _ => throw new Exception("Invalid storage type"),
            },
        ];

        if (!string.IsNullOrWhiteSpace(options.ParentFolder))
            locationParts.Add(options.ParentFolder);

        if (!string.IsNullOrWhiteSpace(name))
            locationParts.Add(name);

        return string.Join("/", locationParts);
    }

    private string cleanseUserId(string userId)
    {
        return userId.Replace("'", "''");
    }

    // /// <summary>
    // /// Gets a file from blob storage.
    // /// </summary>
    // /// <param name="location">The location of the file in blob storage.</param>
    // /// <returns>The stream of the file.</returns>
    // public async Task<Stream?> GetFile(string location)
    // {
    //     var containerClient = getContainerClient();

    //     var blobClient = containerClient.GetBlobClient(location);

    //     if (!await blobClient.ExistsAsync())
    //         return null;

    //     var downloadResult = await blobClient.DownloadStreamingAsync();

    //     return downloadResult.Value.Content;
    // }

    // public async Task<bool> DeleteFile(string location)
    // {
    //     var containerClient = getContainerClient();

    //     var blobClient = containerClient.GetBlobClient(location);

    //     // Check if blob exists before proceeding
    //     if (!await blobClient.ExistsAsync())
    //     {
    //         _logger.LogWarning("Blob does not exist at location: {Location}", location);
    //         return false;
    //     }

    //     // Get blob information before deleting
    //     // var blobUrl = blobClient.Uri.ToString();
    //     var filename = Path.GetFileName(location);

    //     _logger.LogInformation("Deleting file from blob storage: {Location}, filename: {Filename}", location, filename);

    //     // Delete documents from search index first
    //     await _searchIndexService.DeleteDocumentsFromIndexAsync(filename, location);

    //     // Delete the blob
    //     var deleted = await blobClient.DeleteIfExistsAsync();

    //     if (deleted)
    //     {
    //         _logger.LogInformation("Successfully deleted file from blob storage: {Location}", location);
    //     }
    //     else
    //     {
    //         _logger.LogWarning("Failed to delete file from blob storage: {Location}", location);
    //     }

    //     return deleted;
    // }

    // /// <summary>
    // /// Uploads a file to blob storage.
    // /// </summary>
    // /// <param name="stream">The stream to upload.</param>
    // /// <param name="filename">The filename of the file.</param>
    // /// <param name="resourceId">The resource ID of the database record.</param>
    // /// <param name="userId">The user ID of the file.</param>
    // /// <param name="skipIndexing">Whether to skip indexing the file.</param>
    // /// <param name="metadata">The metadata of the file.</param>
    // /// <param name="containerName">The name of the container to upload the file to.</param>
    // /// <param name="parentFolder">The parent folder of the file.</param>
    // /// <returns>The path of the uploaded file.</returns>
    // public async Task<string> UploadFile(Stream stream, string filename, string resourceId, string? userId = null, bool skipIndexing = false, Dictionary<string, string>? metadata = null, string? containerName = null, string? parentFolder = null)
    // {
    //     _logger.LogInformation("Starting file upload for user {UserId}, filename {Filename}", userId, filename);

    //     try
    //     {
    //         var containerClient = getContainerClient();

    //         // Create container if it doesn't exist
    //         await containerClient.CreateIfNotExistsAsync();

    //         var blobPath = $"{(parentFolder is null ? string.Empty : parentFolder + "/") + (userId is null ? "shared" : userId)}/{filename}";
    //         var blobClient = containerClient.GetBlobClient(blobPath);

    //         _logger.LogInformation("Uploading file to blob storage at path {BlobPath}", blobPath);

    //         // Reset stream position if possible
    //         if (stream.CanSeek)
    //         {
    //             stream.Position = 0;
    //         }

    //         await blobClient.UploadAsync(stream, overwrite: true);

    //         var fileMetadata = metadata ?? new Dictionary<string, string>();
    //         fileMetadata["userId"] = userId ?? "";
    //         await blobClient.SetMetadataAsync(metadata);

    //         // Extract, embed, and index the document
    //         if (!skipIndexing)
    //         {
    //             // Reset stream position again for indexing
    //             if (stream.CanSeek)
    //             {
    //                 stream.Position = 0;
    //             }
    //             await _searchIndexService.ExtractEmbedAndIndexFromStreamAsync(stream, filename, userId, blobPath, resourceId);
    //         }

    //         return blobPath;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error uploading file for user {UserId}, filename {Filename}", userId, filename);
    //         throw;
    //     }
    // }

    public async Task<int> DeleteAllUserFiles(string userId)
    {
        _logger.LogInformation("Starting deletion of all files for user {UserId}", userId);

        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cannot delete files for empty user ID");
                return 0;
            }

            // Delete all documents for this user from the search index first
            await _searchIndexService.DeleteAllDocumentsFromIndexAsync(userId);

            // List all blobs for this user using the cleansed user ID as prefix
            var containerClient = await getContainerClient();
            var prefix = $"{cleanseUserId(userId)}/";

            _logger.LogInformation("Listing blobs with prefix {Prefix} for user {UserId}", prefix, userId);

            var blobs = new List<BlobItem>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                blobs.Add(blobItem);
            }

            _logger.LogInformation("Found {BlobCount} blobs to delete for user {UserId}", blobs.Count, userId);

            // Delete each blob
            int deletedCount = 0;
            foreach (var blobItem in blobs)
            {
                try
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var deleted = await blobClient.DeleteIfExistsAsync();

                    if (deleted)
                    {
                        deletedCount++;
                        _logger.LogInformation("Deleted blob: {BlobPath}", blobItem.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Blob did not exist or could not be deleted: {BlobPath}", blobItem.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting blob {BlobName} for user {UserId}", blobItem.Name, userId);
                    // Continue with next blob even if one fails
                }
            }

            _logger.LogInformation("Completed deletion for user {UserId}. Deleted {DeletedCount} of {TotalCount} files", userId, deletedCount, blobs.Count);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting files for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> ReindexFiles(IResourceService resourceService, string? userId = null)
    {
        _logger.LogInformation("Starting reindex for user {UserId}", userId);

        try
        {
            // Delete all documents for this user from the search index
            await _searchIndexService.DeleteAllDocumentsFromIndexAsync(userId);

            // List all blobs in the user's folder
            var containerClient = await getContainerClient();
            var prefix = $"{determineLocation(string.Empty, new StorageOptions
            {
                UserId = userId ?? string.Empty,
                FileOwnership = string.IsNullOrEmpty(userId) ? FileOwnership.Shared : FileOwnership.User,
                StorageType = StorageType.Resources
            })}/";

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

                    // Check if resource exists in database by location
                    var resource = userId is null
                                    ? await resourceService.GetSharedByLocation(blobPath)
                                     : await resourceService.GetForUserByLocation(blobPath, userId!);
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

    private async Task<BlobContainerClient> getContainerClient(bool createIfNotExists = false)
    {
        var containerName = _configuration["storage:container"] ?? CONTAINER_NAME;
        var client = _blobServiceClient.GetBlobContainerClient(containerName);


        if (createIfNotExists)
        {
            // Create container if it doesn't exist
            await client.CreateIfNotExistsAsync();
        }

        return client;
    }

    // /// <summary>
    // /// Uploads an audio file to blob storage without embedding/indexing.
    // /// Used for voice notes and other audio files that don't need to be indexed.
    // /// </summary>
    // public async Task<string> UploadAudioFile(IFormFile file, string blobPath, string? userId = null, Dictionary<string, string>? metadata = null)
    // {
    //     _logger.LogInformation("Starting audio file upload for user {UserId}, path {BlobPath}", userId, blobPath);

    //     try
    //     {
    //         var containerName = getContainerName();
    //         var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

    //         // Create container if it doesn't exist
    //         await containerClient.CreateIfNotExistsAsync();

    //         var blobClient = containerClient.GetBlobClient(blobPath);

    //         _logger.LogInformation("Uploading audio file to blob storage at path {BlobPath}", blobPath);
    //         await using (var stream = file.OpenReadStream())
    //         {
    //             await blobClient.UploadAsync(stream, overwrite: true);
    //         }

    //         // Set metadata
    //         var fileMetadata = metadata ?? new Dictionary<string, string>();
    //         if (userId != null && !fileMetadata.ContainsKey("userId"))
    //         {
    //             fileMetadata["userId"] = userId;
    //         }
    //         if (!fileMetadata.ContainsKey("mimeType"))
    //         {
    //             fileMetadata["mimeType"] = file.ContentType ?? "audio/m4a";
    //         }
    //         await blobClient.SetMetadataAsync(fileMetadata);

    //         return blobPath;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error uploading audio file for user {UserId}, path {BlobPath}", userId, blobPath);
    //         throw;
    //     }
    // }

    // private async Task ExtractEmbedAndIndexAsync(
    //     IFormFile file,
    //     string filename,
    //     string? userId,
    //     string blobPath,
    //     string resourceId)
    // {
    //     await using var stream = file.OpenReadStream();
    //     await _searchIndexService.ExtractEmbedAndIndexFromStreamAsync(stream, filename, userId, blobPath, resourceId);
    // }


    // /// <summary>
    // /// Generates an Azure Search compliant document ID.
    // /// Azure Search IDs can only contain: letters, digits, underscore (_), dash (-), or equal sign (=)
    // /// </summary>
    // private string GenerateSearchDocumentId(string? userId, string filename, int pageNumber)
    // {
    //     // Sanitize userId - replace invalid characters with underscores
    //     var sanitizedUserId = SanitizeForSearchId(userId ?? "shared");

    //     // Sanitize filename - remove extension and sanitize
    //     var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filename);
    //     var sanitizedFilename = SanitizeForSearchId(fileNameWithoutExt);

    //     // Generate a short unique identifier (first 8 chars of GUID, already hex which is safe)
    //     var uniqueId = Guid.NewGuid().ToString("N")[..8];

    //     // Build ID: (userId or "shared")_filename_pageNumber_uniqueId
    //     // All parts are sanitized, and we use underscores as separators
    //     return $"{sanitizedUserId}_{sanitizedFilename}_{pageNumber}_{uniqueId}";
    // }

    // /// <summary>
    // /// Sanitizes a string to only contain characters allowed in Azure Search document IDs:
    // /// letters, digits, underscore (_), dash (-), or equal sign (=)
    // /// Invalid characters are replaced with underscores.
    // /// </summary>
    // private string SanitizeForSearchId(string input)
    // {
    //     if (string.IsNullOrEmpty(input))
    //         return "unknown";

    //     var result = new System.Text.StringBuilder();

    //     foreach (var c in input)
    //     {
    //         // Allow: letters, digits, underscore, dash, equal sign
    //         if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '=')
    //         {
    //             result.Append(c);
    //         }
    //         else
    //         {
    //             // Replace invalid characters with underscore
    //             result.Append('_');
    //         }
    //     }

    //     // Remove consecutive underscores and trim
    //     var sanitized = result.ToString();
    //     while (sanitized.Contains("__"))
    //     {
    //         sanitized = sanitized.Replace("__", "_");
    //     }

    //     return sanitized.Trim('_');
    // }
}