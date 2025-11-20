namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Represents a page of extracted text from a document
/// </summary>
public class DocumentPage
{
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Title { get; set; }
}

/// <summary>
/// Interface for document embedders that extract text from various file types
/// </summary>
public interface IDocumentEmbedder
{
    /// <summary>
    /// Extracts text content from a document stream, splitting multi-page documents into individual pages
    /// </summary>
    /// <param name="stream">The document stream</param>
    /// <param name="filename">The original filename (for context)</param>
    /// <returns>List of document pages with extracted text</returns>
    Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename);
    
    /// <summary>
    /// Gets the file extensions supported by this embedder
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }
}

