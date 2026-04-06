namespace ProjectBrain.Shared.Dtos.Goals;

/// <summary>
/// AI-generated daily goal suggestions. <see cref="Source"/> is "history" when prior incomplete goals existed, else "profile".
/// </summary>
public class GoalSuggestionsResponseDto
{
    public required List<string> Goals { get; init; }

    /// <summary>"history" or "profile"</summary>
    public required string Source { get; init; }
}
