using System.ComponentModel.DataAnnotations;

public class Quiz
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(255)]
    public required string Title { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
}

