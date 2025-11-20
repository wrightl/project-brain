using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for PowerPoint files (.pptx)
/// </summary>
public class PptxDocumentEmbedder : BaseDocumentEmbedder
{
    public PptxDocumentEmbedder(ILogger<PptxDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".pptx" };

    public override Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from PPTX file: {Filename}", filename);
        
        stream.Position = 0;
        var pages = new List<DocumentPage>();
        
        using var presentationDocument = PresentationDocument.Open(stream, false);
        var presentationPart = presentationDocument.PresentationPart;
        
        if (presentationPart == null)
        {
            Logger.LogWarning("Could not read presentation from PPTX file: {Filename}", filename);
            return Task.FromResult(pages);
        }
        
        var presentation = presentationPart.Presentation;
        var slideIdList = presentation?.SlideIdList;
        
        if (slideIdList == null)
        {
            return Task.FromResult(pages);
        }
        
        var title = Path.GetFileNameWithoutExtension(filename);
        
        // Try to extract title from document properties
        // Note: CoreFilePropertiesPart doesn't expose Title directly in this version
        // Using filename as title is acceptable
        
        var pageNumber = 1;
        
        foreach (var slideId in slideIdList.Elements<SlideId>())
        {
            var slidePart = (SlidePart?)presentationPart.GetPartById(slideId.RelationshipId?.Value ?? string.Empty);
            
            if (slidePart?.Slide == null)
                continue;
            
            var slideContent = new System.Text.StringBuilder();
            ExtractTextFromSlide(slidePart.Slide, slideContent);
            
            pages.Add(new DocumentPage
            {
                PageNumber = pageNumber++,
                Content = slideContent.ToString(),
                Title = pageNumber == 2 ? title : $"{title} - Slide {pageNumber - 1}"
            });
        }
        
        Logger.LogInformation("Extracted {PageCount} pages (slides) from PPTX file: {Filename}", pages.Count, filename);
        return Task.FromResult(pages);
    }
    
    private void ExtractTextFromSlide(Slide slide, System.Text.StringBuilder content)
    {
        foreach (var paragraph in slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var paragraphText = new System.Text.StringBuilder();
            foreach (var text in paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            {
                paragraphText.Append(text.Text);
            }
            
            if (paragraphText.Length > 0)
            {
                content.AppendLine(paragraphText.ToString());
            }
        }
    }
}

