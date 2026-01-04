using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectBrain.Database.Models;

public class AgentAction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    public Guid? ConversationId { get; set; } // Link to conversation if applicable

    public Guid? WorkflowId { get; set; } // Link to workflow if applicable

    [Required]
    [StringLength(100)]
    public string ToolName { get; set; } = string.Empty; // e.g., "create_daily_goals"

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ToolParameters { get; set; } = "{}"; // JSON string of parameters

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ToolResult { get; set; } = "{}"; // JSON string of result

    [Required]
    public bool Success { get; set; }

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    [Required]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

