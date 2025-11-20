using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for PDF files (.pdf)
/// </summary>
public class PdfDocumentEmbedder : BaseDocumentEmbedder
{
    public PdfDocumentEmbedder(ILogger<PdfDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".pdf" };

    public override async Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from PDF file: {Filename}", filename);
        
        var pages = new List<DocumentPage>();
        
        // Reset stream position
        stream.Position = 0;
        
        using var document = PdfDocument.Open(stream);
        var title = Path.GetFileNameWithoutExtension(filename);
        
        // Try to extract title from document metadata
        if (document.Information != null && !string.IsNullOrWhiteSpace(document.Information.Title))
        {
            title = document.Information.Title;
        }
        
        int pageNumber = 1;
        foreach (var page in document.GetPages())
        {
            var text = string.Join(" ", page.GetWords().Select(w => w.Text));
            
            pages.Add(new DocumentPage
            {
                PageNumber = pageNumber++,
                Content = text,
                Title = pageNumber == 2 ? title : null // Only set title on first page
            });
        }
        
        Logger.LogInformation("Extracted {PageCount} pages from PDF: {Filename}", pages.Count, filename);
        return pages;
    }
}

