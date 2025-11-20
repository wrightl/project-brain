using System.Text;
using System.Text.RegularExpressions;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for HTML files (.html, .htm)
/// </summary>
public class HtmlDocumentEmbedder : BaseDocumentEmbedder
{
    public HtmlDocumentEmbedder(ILogger<HtmlDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".html", ".htm" };

    public override async Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from HTML file: {Filename}", filename);
        
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var htmlContent = await reader.ReadToEndAsync();
        
        // Extract title from <title> tag or first <h1>
        string? title = null;
        var titleMatch = Regex.Match(htmlContent, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (titleMatch.Success)
        {
            title = Regex.Replace(titleMatch.Groups[1].Value, @"<[^>]+>", "").Trim();
        }
        else
        {
            var h1Match = Regex.Match(htmlContent, @"<h1[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (h1Match.Success)
            {
                title = Regex.Replace(h1Match.Groups[1].Value, @"<[^>]+>", "").Trim();
            }
        }
        
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Path.GetFileNameWithoutExtension(filename);
        }
        
        // Remove script and style tags
        htmlContent = Regex.Replace(htmlContent, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        htmlContent = Regex.Replace(htmlContent, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        // Extract text from HTML tags
        var textContent = Regex.Replace(htmlContent, @"<[^>]+>", " ");
        
        // Clean up whitespace
        textContent = Regex.Replace(textContent, @"\s+", " ");
        textContent = textContent.Trim();
        
        return SplitIntoPages(textContent, title);
    }
}

