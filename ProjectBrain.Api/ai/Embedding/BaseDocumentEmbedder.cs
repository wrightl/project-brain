namespace ProjectBrain.AI.Embedding;

/// <summary>
/// Base class for document embedders with common functionality
/// </summary>
public abstract class BaseDocumentEmbedder : IDocumentEmbedder
{
    protected readonly ILogger Logger;

    protected BaseDocumentEmbedder(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task<List<DocumentPage>> ExtractTextAsync(Stream stream, string filename);
    public abstract IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Splits text into chunks if it exceeds a maximum length per page
    /// </summary>
    protected List<DocumentPage> SplitIntoPages(string content, string? title = null, int maxCharsPerPage = 5000)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<DocumentPage> { new DocumentPage { PageNumber = 1, Content = string.Empty, Title = title } };
        }

        var pages = new List<DocumentPage>();
        var pageNumber = 1;

        if (content.Length <= maxCharsPerPage)
        {
            pages.Add(new DocumentPage
            {
                PageNumber = pageNumber,
                Content = content,
                Title = title
            });
        }
        else
        {
            // Split by paragraphs first, then by sentences if needed
            var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var currentPageContent = new System.Text.StringBuilder();
            var currentPageLength = 0;

            foreach (var paragraph in paragraphs)
            {
                if (currentPageLength + paragraph.Length > maxCharsPerPage && currentPageContent.Length > 0)
                {
                    // Save current page and start new one
                    pages.Add(new DocumentPage
                    {
                        PageNumber = pageNumber++,
                        Content = currentPageContent.ToString(),
                        Title = title
                    });
                    currentPageContent.Clear();
                    currentPageLength = 0;
                }

                if (paragraph.Length > maxCharsPerPage)
                {
                    // Paragraph is too long, split by sentences
                    var sentences = paragraph.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var sentence in sentences)
                    {
                        var sentenceWithPunctuation = sentence.Trim() + ".";
                        if (currentPageLength + sentenceWithPunctuation.Length > maxCharsPerPage && currentPageContent.Length > 0)
                        {
                            pages.Add(new DocumentPage
                            {
                                PageNumber = pageNumber++,
                                Content = currentPageContent.ToString(),
                                Title = title
                            });
                            currentPageContent.Clear();
                            currentPageLength = 0;
                        }
                        currentPageContent.AppendLine(sentenceWithPunctuation);
                        currentPageLength += sentenceWithPunctuation.Length;
                    }
                }
                else
                {
                    currentPageContent.AppendLine(paragraph);
                    currentPageLength += paragraph.Length;
                }
            }

            // Add remaining content as last page
            if (currentPageContent.Length > 0)
            {
                pages.Add(new DocumentPage
                {
                    PageNumber = pageNumber,
                    Content = currentPageContent.ToString(),
                    Title = title
                });
            }
        }

        return pages;
    }
}

