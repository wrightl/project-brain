using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class QuizResponse
{
    public Guid Id { get; set; }

    [Required]
    public Guid QuizId { get; set; }

    [ForeignKey(nameof(QuizId))]
    public Quiz? Quiz { get; set; }

    [Required]
    [StringLength(128)]
    public required string UserId { get; set; }

    // Store answers as JSON string
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string AnswersJson { get; set; }

    public decimal? Score { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper property for JSON serialization
    [NotMapped]
    public Dictionary<string, object> Answers
    {
        get
        {
            if (string.IsNullOrEmpty(AnswersJson))
                return new Dictionary<string, object>();
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(AnswersJson)
                    ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
        set
        {
            AnswersJson = value == null || value.Count == 0
                ? "{}"
                : JsonSerializer.Serialize(value);
        }
    }
}

