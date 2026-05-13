namespace ProjectBrain.Api.Background;

public static class ChatPersistenceConstants
{
    public const string QueueName = "chat-persistence";
    public const int MaxDequeueAttemptsBeforePoison = 5;
}
