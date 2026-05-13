namespace ProjectBrain.Api.Background;

public sealed class ChatPersistenceQueueMessage
{
    public string SchemaVersion { get; set; } = "1";
    public Guid ConversationId { get; set; }
    public string UserId { get; set; } = "";
    public string UserContent { get; set; } = "";
    public string AssistantContent { get; set; } = "";
}
