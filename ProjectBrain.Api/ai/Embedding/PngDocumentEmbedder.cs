namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for PNG image files (.png)
/// Note: This requires OCR capabilities. For now, we'll return a placeholder.
/// In production, you would integrate with Azure Computer Vision or similar OCR service.
/// </summary>
public class PngDocumentEmbedder : BaseDocumentEmbedder
{
    public PngDocumentEmbedder(ILogger<PngDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".png" };

    public override async Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from PNG file: {Filename}", filename);
        
        // TODO: Integrate with Azure Computer Vision OCR or similar service
        // For now, we'll return a placeholder indicating the image was processed
        // but no text was extracted
        
        var title = Path.GetFileNameWithoutExtension(filename);
        var content = $"[Image file: {filename}. OCR text extraction not yet implemented. Please integrate with Azure Computer Vision or similar OCR service.]";
        
        Logger.LogWarning("PNG OCR not implemented. Returning placeholder for: {Filename}", filename);
        
        return new List<DocumentPage>
        {
            new DocumentPage
            {
                PageNumber = 1,
                Content = content,
                Title = title
            }
        };
    }
}

