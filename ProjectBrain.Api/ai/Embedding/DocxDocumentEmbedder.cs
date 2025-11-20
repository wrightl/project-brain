using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for Word documents (.docx)
/// </summary>
public class DocxDocumentEmbedder : BaseDocumentEmbedder
{
    public DocxDocumentEmbedder(ILogger<DocxDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".docx" };

    public override Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from DOCX file: {Filename}", filename);

        stream.Position = 0;
        var pages = new List<DocumentPage>();

        using var wordDocument = WordprocessingDocument.Open(stream, false);
        var body = wordDocument.MainDocumentPart?.Document?.Body;

        if (body == null)
        {
            Logger.LogWarning("Could not read body from DOCX file: {Filename}", filename);
            return Task.FromResult(pages);
        }

        var title = Path.GetFileNameWithoutExtension(filename);

        // Try to extract title from document properties
        // Note: CoreFilePropertiesPart doesn't expose Title directly in this version
        // Using filename as title is acceptable

        var paragraphs = body.Elements<Paragraph>().ToList();
        var currentPageContent = new System.Text.StringBuilder();
        var pageNumber = 1;
        const int maxCharsPerPage = 5000;

        foreach (var paragraph in paragraphs)
        {
            var paragraphText = paragraph.InnerText;

            if (string.IsNullOrWhiteSpace(paragraphText))
                continue;

            // Check if we need to start a new page
            if (currentPageContent.Length + paragraphText.Length > maxCharsPerPage && currentPageContent.Length > 0)
            {
                pages.Add(new DocumentPage
                {
                    PageNumber = pageNumber++,
                    Content = currentPageContent.ToString(),
                    Title = pageNumber == 2 ? title : null
                });
                currentPageContent.Clear();
            }

            currentPageContent.AppendLine(paragraphText);
        }

        // Add remaining content
        if (currentPageContent.Length > 0)
        {
            pages.Add(new DocumentPage
            {
                PageNumber = pageNumber,
                Content = currentPageContent.ToString(),
                Title = pageNumber == 1 ? title : null
            });
        }

        Logger.LogInformation("Extracted {PageCount} pages from DOCX file: {Filename}", pages.Count, filename);
        return Task.FromResult(pages);
    }
}

