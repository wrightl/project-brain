namespace ProjectBrain.Domain.Dtos;

/// <summary>
/// Domain-level DTO for chat messages (to avoid referencing API Models)
/// </summary>
public class AgentChatMessage
{
    public AgentChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
}

public enum AgentChatMessageRole
{
    User,
    Assistant
}

