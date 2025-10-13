using Aspire.Azure.Search.Documents;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;



public class ReindexAzureResources(
        SearchIndexClient searchIndexClient,
        IConfiguration configuration,
        BlobServiceClient blobServiceClient,
        AzureSearchSettings azureSearchSettings,
        ILogger<ReindexAzureResources> logger)
{
    private readonly SearchIndexClient _searchIndexClient = searchIndexClient;
    private readonly AzureSearchSettings _azureSearchSettings = azureSearchSettings;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<ReindexAzureResources> _logger = logger;
    private readonly BlobServiceClient _blobServiceClient = blobServiceClient;


    [TickerFunction(functionName: nameof(ReindexResources), cronExpression: "* * * * *")]
    public async Task ReindexResources()
    {
        var lockBlob = GetBlobClient("reindex.lock");
        try
        {
            // Try to acquire lock
            _logger.LogInformation("Search settings: {settings}", _azureSearchSettings);
            if (await lockBlob.ExistsAsync())
            {
                _logger.LogInformation("Reindex already running, skipping this instance.");
                return;
            }
            await lockBlob.UploadAsync(new BinaryData(DateTimeOffset.UtcNow.ToString("O")), overwrite: false);

            var lastRunTimestamp = await GetRunTimestampAsync("last_reindex_timestamp.txt");
            var lastFileChange = await GetRunTimestampAsync("last_filechange_timestamp.txt");

            if (!lastFileChange.HasValue || (lastRunTimestamp.HasValue && (lastFileChange < lastRunTimestamp || DateTimeOffset.UtcNow - lastRunTimestamp < TimeSpan.FromSeconds(181))))
            {
                _logger.LogInformation($"Last reindex ran at {lastRunTimestamp}, last file changed at {lastFileChange}, skipping this run.");
                return;
            }

            try
            {
                var indexName = "user-resources-indexer";
                _logger.LogInformation("Running Azure Search indexer {IndexerName}", indexName);

                var indexer = new SearchIndexerClient(_searchIndexClient.Endpoint, new DefaultAzureCredential());
                await indexer.RunIndexerAsync(indexName);

                _logger.LogInformation("Reindex completed successfully.");

                await SaveRunTimestampAsync(DateTimeOffset.UtcNow, "last_reindex_timestamp.txt");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reindex failed.");
                throw;
            }
        }
        finally
        {
            // Release lock
            try
            {
                await lockBlob.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release reindex lock.");
            }
        }
    }

    private async Task<DateTimeOffset?> GetRunTimestampAsync(string fileName)
    {
        BlobClient timestampBlob = GetBlobClient(fileName);
        if (await timestampBlob.ExistsAsync())
        {
            var content = await timestampBlob.DownloadContentAsync();
            var timestampString = content.Value.Content.ToString();
            _logger.LogInformation($"Read timestamp from {fileName}: {timestampString}");
            if (DateTimeOffset.TryParse(timestampString, out var timestamp))
            {
                return timestamp;
            }
        }
        return null;
    }

    private BlobClient GetBlobClient(string fileName)
    {
        var containerName = _configuration["storage:container"] ?? "resources";
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var timestampBlob = containerClient.GetBlobClient(fileName);
        return timestampBlob;
    }

    private async Task SaveRunTimestampAsync(DateTimeOffset timestamp, string fileName)
    {
        BlobClient timestampBlob = GetBlobClient(fileName);
        await timestampBlob.UploadAsync(new BinaryData(timestamp.ToString("O")), true);
    }
}