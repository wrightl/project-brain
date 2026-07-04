using System.ComponentModel.DataAnnotations;

public class Conversation
{
    public Guid Id { get; set; }
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;
    [StringLength(128)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Rolling summary of the conversation for prompt injection.</summary>
    [StringLength(4000)]
    public string? ContextSummary { get; set; }

    /// <summary>Message count when <see cref="ContextSummary"/> was last updated.</summary>
    public int SummaryMessageCount { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}