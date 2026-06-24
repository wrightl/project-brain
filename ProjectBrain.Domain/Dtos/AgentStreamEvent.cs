namespace ProjectBrain.Domain.Dtos;

public sealed class AgentStreamEvent
{
    public required string Type { get; init; }
    public object? Value { get; init; }
}
