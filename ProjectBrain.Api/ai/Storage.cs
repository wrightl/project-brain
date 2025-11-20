using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using OpenAI;
using OpenAI.Embeddings;
using ProjectBrain.AI;
using ProjectBrain.AI.Embedding;

public class Storage
{
    private readonly IConfiguration _configuration;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<Storage> _logger;
    private readonly DocumentEmbedderFactory _embedderFactory;
    private readonly OpenAIClient _openAIClient;
    private readonly SearchIndexClient _searchIndexClient;

    public Storage(
        IConfiguration configuration,
        BlobServiceClient blobServiceClient,
        ILogger<Storage> logger,
        DocumentEmbedderFactory embedderFactory,
        OpenAIClient openAIClient,
        SearchIndexClient searchIndexClient)
    {
        _configuration = configuration;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _embedderFactory = embedderFactory;
        _openAIClient = openAIClient;
        _searchIndexClient = searchIndexClient;
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
        var blobUrl = blobClient.Uri.ToString();
        var filename = Path.GetFileName(location);

        _logger.LogInformation("Deleting file from blob storage: {Location}, filename: {Filename}", location, filename);

        // Delete documents from search index first
        await DeleteDocumentsFromIndexAsync(filename, blobUrl);

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

    private async Task DeleteDocumentsFromIndexAsync(string filename, string storageUrl)
    {
        try
        {
            _logger.LogInformation("Deleting documents from search index for file: {Filename}, URL: {StorageUrl}", filename, storageUrl);

            var searchClient = _searchIndexClient.GetSearchClient(AzureOpenAI.SEARCH_INDEX_NAME);

            // Search for all documents matching this file
            // Use storageUrl as the identifier since it's unique per blob
            var escapedUrl = storageUrl.Replace("'", "''");
            var filter = $"storageUrl eq '{escapedUrl}'";

            _logger.LogInformation("Searching for documents with filter: {Filter}", filter);

            var searchOptions = new SearchOptions
            {
                Filter = filter,
                Size = 1000 // Maximum documents per page
            };
            searchOptions.Select.Add("id"); // Only retrieve the id field

            var searchResults = await searchClient.SearchAsync<SearchDocument>("*", searchOptions);

            var documentIdsToDelete = new List<string>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                if (result.Document.ContainsKey("id") && result.Document["id"] != null)
                {
                    documentIdsToDelete.Add(result.Document["id"].ToString()!);
                }
            }

            if (documentIdsToDelete.Count == 0)
            {
                _logger.LogInformation("No documents found in search index for file: {Filename}", filename);
                return;
            }

            _logger.LogInformation("Found {DocumentCount} documents to delete for file: {Filename}", documentIdsToDelete.Count, filename);

            // Delete documents in batches (Azure Search supports up to 1000 documents per batch)
            const int batchSize = 1000;
            for (int i = 0; i < documentIdsToDelete.Count; i += batchSize)
            {
                var batch = documentIdsToDelete.Skip(i).Take(batchSize);
                var deleteDocuments = batch.Select(id => new SearchDocument { ["id"] = id }).ToList();

                var deleteBatch = IndexDocumentsBatch.Delete(deleteDocuments);
                var deleteResult = await searchClient.IndexDocumentsAsync(deleteBatch);

                var successCount = deleteResult.Value.Results.Count(r => r.Succeeded);
                var failedCount = deleteResult.Value.Results.Count(r => !r.Succeeded);

                _logger.LogInformation(
                    "Deleted batch of documents for file: {Filename}. Success: {SuccessCount}, Failed: {FailedCount}",
                    filename,
                    successCount,
                    failedCount);

                // Log any failures
                foreach (var result in deleteResult.Value.Results.Where(r => !r.Succeeded))
                {
                    _logger.LogError("Failed to delete document {Key}: {ErrorMessage}", result.Key, result.ErrorMessage);
                }
            }

            _logger.LogInformation("Completed deleting {DocumentCount} documents from search index for file: {Filename}", documentIdsToDelete.Count, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting documents from search index for file: {Filename}", filename);
            // Don't throw - we don't want to fail the blob deletion if index deletion fails
        }
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

            // Create container if it doesn't exist
            await containerClient.CreateIfNotExistsAsync();

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

            // This is being executed as a background task every 1 minute. Update last file change time instead
            // var timestampBlob = containerClient.GetBlobClient("last_filechange_timestamp.txt");
            // await timestampBlob.UploadAsync(new BinaryData(DateTimeOffset.UtcNow.ToString("O")), true);

            // Extract, embed, and index the document
            await ExtractEmbedAndIndexAsync(file, filename, userId, blobClient.Uri.ToString(), blobPath);

            return blobPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for user {UserId}, filename {Filename}", userId, filename);
            throw;
        }
    }

    private async Task ExtractEmbedAndIndexAsync(
        IFormFile file,
        string filename,
        string userId,
        string storageUrl,
        string blobPath)
    {
        try
        {
            // Check if file type is supported
            if (!_embedderFactory.IsSupported(filename))
            {
                _logger.LogWarning("File type not supported for embedding: {Filename}", filename);
                return;
            }

            // Get the appropriate embedder
            var embedder = _embedderFactory.GetEmbedder(filename);
            if (embedder == null)
            {
                _logger.LogWarning("No embedder found for file: {Filename}", filename);
                return;
            }

            _logger.LogInformation("Extracting text from file: {Filename}", filename);

            // Extract text from the document
            await using var stream = file.OpenReadStream();
            var pages = await embedder.ExtractTextAsync(stream, filename);

            if (pages.Count == 0)
            {
                _logger.LogWarning("No content extracted from file: {Filename}", filename);
                return;
            }

            _logger.LogInformation("Extracted {PageCount} pages from file: {Filename}", pages.Count, filename);

            // Generate embeddings and index each page
            var embedClient = _openAIClient.GetEmbeddingClient("openai-embed-deployment");
            var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
            var searchClient = _searchIndexClient.GetSearchClient(AzureOpenAI.SEARCH_INDEX_NAME);

            var documentsToIndex = new List<SearchDocument>();

            foreach (var page in pages)
            {
                if (string.IsNullOrWhiteSpace(page.Content))
                {
                    _logger.LogWarning("Skipping empty page {PageNumber} from file: {Filename}", page.PageNumber, filename);
                    continue;
                }

                // Generate embedding for the page content
                _logger.LogInformation("Generating embedding for page {PageNumber} of {Filename}", page.PageNumber, filename);
                var embedResponse = await embedClient.GenerateEmbeddingAsync(page.Content, embeddingOptions);
                var embeddingFloats = embedResponse.Value.ToFloats();
                // Convert ReadOnlyMemory<float> to List<float>
                var embedding = new List<float>();
                foreach (var f in embeddingFloats.Span)
                {
                    embedding.Add(f);
                }

                // Create search document with Azure Search compliant ID
                // Azure Search IDs can only contain: letters, digits, underscore (_), dash (-), or equal sign (=)
                var documentId = GenerateSearchDocumentId(userId, filename, page.PageNumber);
                var searchDocument = new SearchDocument
                {
                    ["id"] = documentId,
                    ["content"] = page.Content,
                    ["category"] = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant(),
                    ["sourcepage"] = page.PageNumber.ToString(),
                    ["sourcefile"] = filename,
                    ["storageUrl"] = storageUrl,
                    ["ownerId"] = userId,
                    ["embedding"] = embedding
                };

                documentsToIndex.Add(searchDocument);
            }

            // Batch index all documents
            if (documentsToIndex.Count > 0)
            {
                _logger.LogInformation("Indexing {DocumentCount} documents for file: {Filename}", documentsToIndex.Count, filename);

                var batch = IndexDocumentsBatch.Upload(documentsToIndex);
                var indexResult = await searchClient.IndexDocumentsAsync(batch);

                _logger.LogInformation(
                    "Indexed {DocumentCount} documents for file: {Filename}. Success: {SuccessCount}, Failed: {FailedCount}",
                    documentsToIndex.Count,
                    filename,
                    indexResult.Value.Results.Count(r => r.Succeeded),
                    indexResult.Value.Results.Count(r => !r.Succeeded));

                // Log any failures
                foreach (var result in indexResult.Value.Results.Where(r => !r.Succeeded))
                {
                    _logger.LogError("Failed to index document {Key}: {ErrorMessage}", result.Key, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting, embedding, or indexing file: {Filename}", filename);
            // Don't throw - we don't want to fail the upload if indexing fails
        }
    }

    /// <summary>
    /// Generates an Azure Search compliant document ID.
    /// Azure Search IDs can only contain: letters, digits, underscore (_), dash (-), or equal sign (=)
    /// </summary>
    private string GenerateSearchDocumentId(string userId, string filename, int pageNumber)
    {
        // Sanitize userId - replace invalid characters with underscores
        var sanitizedUserId = SanitizeForSearchId(userId);

        // Sanitize filename - remove extension and sanitize
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filename);
        var sanitizedFilename = SanitizeForSearchId(fileNameWithoutExt);

        // Generate a short unique identifier (first 8 chars of GUID, already hex which is safe)
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        // Build ID: userId_filename_pageNumber_uniqueId
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