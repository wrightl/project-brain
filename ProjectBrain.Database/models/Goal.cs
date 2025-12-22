using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectBrain.Database.Models;

public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "date")]
    public DateOnly Date { get; set; }

    [Required]
    public int Index { get; set; } // 0, 1, or 2

    [Required]
    [StringLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string Message { get; set; } = string.Empty;

    [Required]
    public bool Completed { get; set; } = false;

    public DateTime? CompletedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

