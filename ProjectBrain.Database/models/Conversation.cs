using System.ComponentModel.DataAnnotations;

public class Conversation
{
    public Guid Id { get; set; }
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;
    [StringLength(128)]
    public string Title { get; set; } = string.Empty;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}