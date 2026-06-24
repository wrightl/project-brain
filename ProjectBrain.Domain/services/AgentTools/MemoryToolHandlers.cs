namespace ProjectBrain.Domain.AgentTools;

using ProjectBrain.Domain;

public sealed class ListMyMemoriesToolHandler : IAgentToolHandler
{
    public string Name => "list_my_memories";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "List facts and episodes the assistant remembers about the user.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>(),
                ["required"] = Array.Empty<string>()
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.UserMemoryService is null)
        {
            throw new InvalidOperationException("User memory service is not available");
        }

        var memories = await context.UserMemoryService.ListAsync(context.UserId, cancellationToken);
        return new
        {
            success = true,
            facts = memories.Facts.Select(f => new
            {
                id = f.Id,
                type = "fact",
                content = Truncate(f.Content, 300),
                category = f.Category,
                isPinned = f.IsPinned
            }),
            episodes = memories.Episodes.Select(e => new
            {
                id = e.Id,
                type = "episode",
                summary = Truncate(e.Summary, 300),
                topic = e.Topic,
                outcome = e.Outcome,
                isPinned = e.IsPinned
            })
        };
    }

    private static string Truncate(string? text, int max)
        => string.IsNullOrEmpty(text) || text.Length <= max ? text ?? string.Empty : text[..max] + "...";
}

public sealed class RememberFactToolHandler : IAgentToolHandler
{
    public string Name => "remember_fact";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Save an explicit fact the user wants remembered for future conversations.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Fact to remember" },
                    ["category"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional category" }
                },
                ["required"] = new[] { "content" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.AgentMemoryWriteService is null)
        {
            throw new InvalidOperationException("Agent memory write service is not available");
        }

        var content = AgentToolParameterParser.ParseString(parameters.GetValueOrDefault("content"), "content");
        var category = parameters.TryGetValue("category", out var cat) ? cat?.ToString() : null;

        var result = await context.AgentMemoryWriteService.RememberFactAsync(
            context.UserId,
            content,
            category,
            context.ConversationId,
            cancellationToken);

        return new
        {
            success = true,
            message = "Fact remembered",
            fact = new { id = result.Id, content = result.Content, category = result.Category }
        };
    }
}

public sealed class ForgetMemoryToolHandler : IAgentToolHandler
{
    public string Name => "forget_memory";
    public bool RequiresConfirmation => true;

    public string? BuildConfirmationPreview(Dictionary<string, object> parameters)
    {
        var memoryType = parameters.GetValueOrDefault("memoryType")?.ToString() ?? "memory";
        var memoryId = parameters.GetValueOrDefault("memoryId")?.ToString() ?? "unknown";
        return $"Forget {memoryType} {memoryId}";
    }

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Delete a remembered fact or episode. Requires user confirmation.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["memoryId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Memory ID (GUID)" },
                    ["memoryType"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["enum"] = new[] { "fact", "episode" },
                        ["description"] = "Whether this is a fact or episode"
                    }
                },
                ["required"] = new[] { "memoryId", "memoryType" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.UserMemoryService is null)
        {
            throw new InvalidOperationException("User memory service is not available");
        }

        var memoryId = AgentToolParameterParser.ParseGuid(parameters.GetValueOrDefault("memoryId"), "memoryId");
        var memoryType = AgentToolParameterParser.ParseString(parameters.GetValueOrDefault("memoryType"), "memoryType").ToLowerInvariant();

        var deleted = memoryType switch
        {
            "fact" => await context.UserMemoryService.DeleteFactAsync(context.UserId, memoryId, cancellationToken),
            "episode" => await context.UserMemoryService.DeleteEpisodeAsync(context.UserId, memoryId, cancellationToken),
            _ => throw new ArgumentException("memoryType must be 'fact' or 'episode'")
        };

        return new
        {
            success = deleted,
            message = deleted ? "Memory forgotten" : "Memory not found",
            memoryId,
            memoryType
        };
    }
}
