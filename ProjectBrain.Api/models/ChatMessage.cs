namespace ProjectBrain.Models;

public class ChatMessage
{
    public ChatMessageRole Role { get; set; } = ChatMessageRole.User;
    public string Content { get; set; } = string.Empty;
}

public enum ChatMessageRole
{
    User,
    Assistant
}