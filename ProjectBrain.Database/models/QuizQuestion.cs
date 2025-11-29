using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class QuizQuestion
{
    public Guid Id { get; set; }

    [Required]
    public Guid QuizId { get; set; }

    [ForeignKey(nameof(QuizId))]
    public Quiz? Quiz { get; set; }

    [Required]
    [StringLength(1000)]
    public required string Label { get; set; }

    [Required]
    [StringLength(50)]
    public required string InputType { get; set; } // text, number, email, date, choice, multipleChoice, scale, textarea, tel, url

    public bool Mandatory { get; set; } = false;

    public bool Visible { get; set; } = true;

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    // Store choices as JSON string (SQL Server doesn't have native JSONB)
    [Column(TypeName = "nvarchar(max)")]
    public string? ChoicesJson { get; set; }

    [StringLength(255)]
    public string? Placeholder { get; set; }

    [StringLength(500)]
    public string? Hint { get; set; }

    public int QuestionOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper properties for JSON serialization
    [NotMapped]
    public List<string>? Choices
    {
        get
        {
            if (string.IsNullOrEmpty(ChoicesJson))
                return null;
            try
            {
                return JsonSerializer.Deserialize<List<string>>(ChoicesJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            ChoicesJson = value == null || value.Count == 0
                ? null
                : JsonSerializer.Serialize(value);
        }
    }
}

