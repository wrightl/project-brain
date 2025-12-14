using System.Text;
using System.Text.Json;

namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Embedder for JSON files (.json)
/// </summary>
public class JsonDocumentEmbedder : BaseDocumentEmbedder
{
    public JsonDocumentEmbedder(ILogger<JsonDocumentEmbedder> logger) : base(logger)
    {
    }

    public override IEnumerable<string> SupportedExtensions => new[] { ".json" };

    public override async Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename)
    {
        Logger.LogInformation("Extracting text from JSON file: {Filename}", filename);

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var jsonContent = await reader.ReadToEndAsync();

        try
        {
            // Parse and format JSON for better readability
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // Serialize the root element to get formatted JSON
            var formattedJson = JsonSerializer.Serialize(jsonDoc.RootElement, options);

            var title = Path.GetFileNameWithoutExtension(filename);
            return SplitIntoPages(formattedJson, title);
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Failed to parse JSON file: {Filename}. Using raw content.", filename);
            // If JSON parsing fails, return the raw content
            var title = Path.GetFileNameWithoutExtension(filename);
            return SplitIntoPages(jsonContent, title);
        }
    }
}

