using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using ProjectBrain.AI.Embedding;
using OpenAI;
using OpenAI.Embeddings;
using Azure;

public class AzureSearchClientServices(
        SearchIndexClient searchIndexClient,
        ILogger<AzureSearchClientServices> logger,
        DocumentEmbedderFactory embedderFactory,
        OpenAIClient openAIClient)
{
    public SearchIndexClient SearchIndexClient { get; } = searchIndexClient;
    public ILogger<AzureSearchClientServices> Logger { get; } = logger;
    public DocumentEmbedderFactory EmbedderFactory { get; } = embedderFactory;
    public OpenAIClient OpenAIClient { get; } = openAIClient;
}

public class AzureSearchClient(AzureSearchClientServices services) : ISearchIndexService
{
    public AzureSearchClientServices Services { get; } = services;

    public Task<Response<SearchResults<SearchDocument>>> SearchAsync(string query, SearchOptions searchOptions)
    {
        var searchClient = Services.SearchIndexClient.GetSearchClient(Constants.SEARCH_INDEX_NAME);
        return searchClient.SearchAsync<SearchDocument>(query, searchOptions);
    }

    public async Task ExtractEmbedAndIndexFromStreamAsync(
        Stream stream,
        string filename,
        string? userId,
        string blobPath,
        string resourceId)
    {
        try
        {
            // Check if file type is supported
            if (!services.EmbedderFactory.IsSupported(filename))
            {
                services.Logger.LogWarning("File type not supported for embedding: {Filename}", filename);
                return;
            }

            // TODO - Reinstate this if we decide to only allow unique filenames
            // Delete from index in case it's a reupload of an existing file
            // await DeleteDocumentsFromIndexAsync(filename, blobPath);

            // Get the appropriate embedder
            var embedder = services.EmbedderFactory.GetEmbedder(filename);
            if (embedder == null)
            {
                services.Logger.LogWarning("No embedder found for file: {Filename}", filename);
                return;
            }

            services.Logger.LogInformation("Extracting text from file: {Filename}", filename);

            // Extract text from the document
            // Reset stream position to beginning in case it was already read
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
            var pages = await embedder.ExtractTextAsync(stream, filename);

            if (pages.Count == 0)
            {
                services.Logger.LogWarning("No content extracted from file: {Filename}", filename);
                return;
            }

            services.Logger.LogInformation("Extracted {PageCount} pages from file: {Filename}", pages.Count, filename);

            // Generate embeddings and index each page
            var embedClient = services.OpenAIClient.GetEmbeddingClient("openai-embed-deployment");
            var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = 1536 };
            var searchClient = services.SearchIndexClient.GetSearchClient(Constants.SEARCH_INDEX_NAME);

            var documentsToIndex = new List<SearchDocument>();

            foreach (var page in pages)
            {
                if (string.IsNullOrWhiteSpace(page.Content))
                {
                    services.Logger.LogWarning("Skipping empty page {PageNumber} from file: {Filename}", page.PageNumber, filename);
                    continue;
                }

                // Generate embedding for the page content
                services.Logger.LogInformation("Generating embedding for page {PageNumber} of {Filename}", page.PageNumber, filename);
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
                // var documentId = GenerateSearchDocumentId(userId, filename, page.PageNumber);
                var searchDocument = new SearchDocument
                {
                    ["id"] = $"{resourceId}_{page.PageNumber}",
                    ["content"] = page.Content,
                    ["category"] = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant(),
                    ["sourcepage"] = page.PageNumber.ToString(),
                    ["sourcefile"] = filename,
                    ["storageUrl"] = blobPath,
                    ["ownerId"] = userId,
                    ["embedding"] = embedding
                };

                documentsToIndex.Add(searchDocument);
            }

            // Batch index all documents
            if (documentsToIndex.Count > 0)
            {
                services.Logger.LogInformation("Indexing {DocumentCount} documents for file: {Filename}", documentsToIndex.Count, filename);

                var batch = IndexDocumentsBatch.Upload(documentsToIndex);
                var indexResult = await searchClient.IndexDocumentsAsync(batch);

                services.Logger.LogInformation(
                    "Indexed {DocumentCount} documents for file: {Filename}. Success: {SuccessCount}, Failed: {FailedCount}",
                    documentsToIndex.Count,
                    filename,
                    indexResult.Value.Results.Count(r => r.Succeeded),
                    indexResult.Value.Results.Count(r => !r.Succeeded));

                // Log any failures
                foreach (var result in indexResult.Value.Results.Where(r => !r.Succeeded))
                {
                    services.Logger.LogError("Failed to index document {Key}: {ErrorMessage}", result.Key, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error extracting, embedding, or indexing file: {Filename}", filename);
            // Don't throw - we don't want to fail the upload if indexing fails
        }
    }

    public async Task DeleteAllDocumentsFromIndexAsync(string? userId)
    {
        try
        {
            services.Logger.LogInformation("Deleting all documents from search index for user: {UserId}", userId);

            var searchClient = services.SearchIndexClient.GetSearchClient(Constants.SEARCH_INDEX_NAME);

            // Search for all documents matching this user or shared
            var filter = userId is null ? "ownerId eq '' or ownerId eq null" : $"ownerId eq '{userId.Replace("'", "''")}'";

            services.Logger.LogInformation("Searching for documents with filter: {Filter}", filter);

            var searchOptions = new SearchOptions
            {
                Filter = filter,
                Size = 1000 // Maximum documents per page
            };
            searchOptions.Select.Add("id"); // Retrieve the id field
            // searchOptions.Select.Add("sourcefile"); // Retrieve the sourcefile field

            var searchResults = await searchClient.SearchAsync<SearchDocument>("*", searchOptions);

            var documentIdsToDelete = new List<string>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                if (shouldRemoveDocumentFromIndex(result.Document))
                    documentIdsToDelete.Add(result.Document["id"].ToString()!);
            }

            if (documentIdsToDelete.Count == 0)
            {
                services.Logger.LogInformation("No documents found in search index for user: {UserId}", userId);
                return;
            }

            services.Logger.LogInformation("Found {DocumentCount} documents to delete for user: {UserId}", documentIdsToDelete.Count, userId);

            await deleteDocumentsFromIndexAsync(searchClient, documentIdsToDelete);

            services.Logger.LogInformation("Completed deleting {DocumentCount} documents from search index for user: {UserId}", documentIdsToDelete.Count, userId);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting documents from search index for user: {UserId}", userId);
            // Don't throw - we want to continue with reindexing even if deletion fails
        }
    }

    public async Task DeleteDocumentsFromIndexAsync(string filename, string location)
    {
        try
        {
            services.Logger.LogInformation("Deleting documents from search index for file: {Filename}, URL: {Location}", filename, location);

            var searchClient = services.SearchIndexClient.GetSearchClient(Constants.SEARCH_INDEX_NAME);

            // Search for all documents matching this file
            // Use location as the identifier since it's unique per blob
            var escapedUrl = location.Replace("'", "''");
            var filter = $"storageUrl eq '{escapedUrl}'";

            services.Logger.LogInformation("Searching for documents with filter: {Filter}", filter);

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
                services.Logger.LogInformation("No documents found in search index for file: {Filename}", filename);
                return;
            }

            services.Logger.LogInformation("Found {DocumentCount} documents to delete for file: {Filename}", documentIdsToDelete.Count, filename);

            await deleteDocumentsFromIndexAsync(searchClient, documentIdsToDelete);

            services.Logger.LogInformation("Completed deleting {DocumentCount} documents from search index for file: {Filename}", documentIdsToDelete.Count, filename);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting documents from search index for file: {Filename}", filename);
            // Don't throw - we don't want to fail the blob deletion if index deletion fails
        }
    }

    private async Task deleteDocumentsFromIndexAsync(SearchClient searchClient, List<string> documentIdsToDelete)
    {
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

            services.Logger.LogInformation(
                "Deleted batch of documents. Success: {SuccessCount}, Failed: {FailedCount}",
                successCount,
                failedCount);

            // Log any failures
            foreach (var result in deleteResult.Value.Results.Where(r => !r.Succeeded))
            {
                services.Logger.LogError("Failed to delete document {Key}: {ErrorMessage}", result.Key, result.ErrorMessage);
            }
        }
    }

    private static bool shouldRemoveDocumentFromIndex(SearchDocument document)
    {
        return document.ContainsKey("id") && document["id"] != null;
    }
}

public interface ISearchIndexService
{
    Task<Response<SearchResults<SearchDocument>>> SearchAsync(string query, SearchOptions searchOptions);
    Task DeleteDocumentsFromIndexAsync(string filename, string location);
    Task DeleteAllDocumentsFromIndexAsync(string? userId);
    Task ExtractEmbedAndIndexFromStreamAsync(Stream stream, string filename, string? userId, string blobPath, string resourceId);
}