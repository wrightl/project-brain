using System.Text;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for Markdown files (.md)
/// </summary>
public class MarkdownDocumentEmbedder : BaseDocumentEmbedder
{
    public MarkdownDocumentEmbedder(ILogger<MarkdownDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".md", ".markdown" };

    public override async Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from Markdown file: {Filename}", filename);
        
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        
        // Extract title from first heading if available
        string? title = null;
        using var lineReader = new StringReader(content);
        var firstLine = await lineReader.ReadLineAsync();
        if (firstLine != null && firstLine.TrimStart().StartsWith("#"))
        {
            title = firstLine.TrimStart('#').Trim();
        }
        else
        {
            title = Path.GetFileNameWithoutExtension(filename);
        }
        
        return SplitIntoPages(content, title);
    }
}

