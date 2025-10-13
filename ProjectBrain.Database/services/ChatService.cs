public class ChatService : IChatService
{
    private readonly AppDbContext _context;

    public ChatService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessage>> AddMany(List<ChatMessage> chatMessages)
    {
        // Store user messages in DB
        _context.ChatMessages.AddRange(chatMessages);
        await _context.SaveChangesAsync();
        return chatMessages;
    }

    public async Task<ChatMessage> Add(ChatMessage chatMessage)
    {
        // Store user messages in DB
        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();
        return chatMessage;
    }

    // public async Task<IEnumerable<ChatMessage>> GetByConversationId(Guid conversationId)
    // {
    //     return await Task.FromResult(_context.ChatMessages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.CreatedAt).AsEnumerable());
    // }
}

public interface IChatService
{
    Task<List<ChatMessage>> AddMany(List<ChatMessage> chatMessages);
    Task<ChatMessage> Add(ChatMessage chatMessage);
    // Task<IEnumerable<ChatMessage>> GetByConversationId(Guid conversationId);
}