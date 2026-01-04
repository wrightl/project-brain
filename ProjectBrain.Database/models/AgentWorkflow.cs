using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectBrain.Database.Models;

public class AgentWorkflow
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(128)]
    public string UserId { get; set; } = string.Empty;

    public Guid? ConversationId { get; set; }

    [Required]
    [StringLength(100)]
    public string WorkflowType { get; set; } = string.Empty; // e.g., "goal_creation", "multi_step_task"

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "active"; // "active", "paused", "completed", "failed"

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string CurrentState { get; set; } = "{}"; // JSON string of current workflow state

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ToolExecutionHistory { get; set; } = "[]"; // JSON array of executed tools

    [Required]
    public int CurrentStep { get; set; } = 0;

    [Required]
    public int TotalSteps { get; set; } = 0; // Total expected steps (if known)

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

