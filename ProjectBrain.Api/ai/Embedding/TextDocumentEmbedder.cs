using System.Text;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for plain text files (.txt)
/// </summary>
public class TextDocumentEmbedder : BaseDocumentEmbedder
{
    public TextDocumentEmbedder(ILogger<TextDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".txt" };

    public override async Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from TXT file: {Filename}", filename);
        
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        
        var title = Path.GetFileNameWithoutExtension(filename);
        return SplitIntoPages(content, title);
    }
}

