namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Factory for creating document embedders based on file extension
/// </summary>
public class DocumentEmbedderFactory
{
    private readonly Dictionary<string, IDocumentEmbedder> _embedders;
    private readonly ILogger<DocumentEmbedderFactory> _logger;

    public DocumentEmbedderFactory(
        IEnumerable<IDocumentEmbedder> embedders,
        ILogger<DocumentEmbedderFactory> logger)
    {
        _logger = logger;
        _embedders = new Dictionary<string, IDocumentEmbedder>(StringComparer.OrdinalIgnoreCase);
        
        // Register all embedders by their supported extensions
        foreach (var embedder in embedders)
        {
            foreach (var extension in embedder.SupportedExtensions)
            {
                if (_embedders.ContainsKey(extension))
                {
                    _logger.LogWarning("Duplicate embedder registration for extension: {Extension}", extension);
                }
                else
                {
                    _embedders[extension] = embedder;
                }
            }
        }
        
        _logger.LogInformation("DocumentEmbedderFactory initialized with {Count} embedders", _embedders.Count);
    }

    /// <summary>
    /// Gets the appropriate embedder for a file based on its extension
    /// </summary>
    /// <param name="filename">The filename with extension</param>
    /// <returns>The embedder for the file type, or null if not supported</returns>
    public IDocumentEmbedder? GetEmbedder(string filename)
    {
        var extension = Path.GetExtension(filename);
        
        if (string.IsNullOrEmpty(extension))
        {
            _logger.LogWarning("No extension found for filename: {Filename}", filename);
            return null;
        }
        
        if (_embedders.TryGetValue(extension, out var embedder))
        {
            return embedder;
        }
        
        _logger.LogWarning("No embedder found for extension: {Extension}, filename: {Filename}", extension, filename);
        return null;
    }

    /// <summary>
    /// Checks if a file type is supported
    /// </summary>
    public bool IsSupported(string filename)
    {
        var extension = Path.GetExtension(filename);
        return !string.IsNullOrEmpty(extension) && _embedders.ContainsKey(extension);
    }
}

