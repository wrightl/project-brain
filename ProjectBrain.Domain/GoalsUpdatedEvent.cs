namespace ProjectBrain.Domain;

/// <summary>
/// Event payload sent over SSE when a user's goals are mutated.
/// </summary>
public class GoalsUpdatedEvent
{
    /// <summary>
    /// When the goals were updated (ISO 8601).
    /// </summary>
    public string? UpdatedAt { get; set; }
}
