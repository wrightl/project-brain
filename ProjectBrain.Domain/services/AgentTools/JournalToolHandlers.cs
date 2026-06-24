namespace ProjectBrain.Domain.AgentTools;

using System.Text.Json;
using ProjectBrain.Domain;

public sealed class CreateJournalEntryToolHandler : IAgentToolHandler
{
    public string Name => "create_journal_entry";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Create a journal entry for the user with the given content.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["content"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Journal entry content"
                    }
                },
                ["required"] = new[] { "content" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.JournalAgentService is null)
        {
            throw new InvalidOperationException("Journal agent service is not available");
        }

        var content = AgentToolParameterParser.ParseString(parameters.GetValueOrDefault("content"), "content");
        var entry = await context.JournalAgentService.CreateEntryAsync(context.UserId, content, cancellationToken);

        return new
        {
            success = true,
            message = "Journal entry created",
            entry = new
            {
                id = entry.Id,
                summary = entry.Summary,
                createdAt = entry.CreatedAt
            }
        };
    }
}

public sealed class GetRecentJournalEntriesToolHandler : IAgentToolHandler
{
    public string Name => "get_recent_journal_entries";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Get the user's recent journal entries (summaries only, not full content).",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["count"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["minimum"] = 1,
                        ["maximum"] = 10,
                        ["description"] = "Number of entries to return (default 3)"
                    }
                },
                ["required"] = Array.Empty<string>()
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.JournalEntryService is null)
        {
            throw new InvalidOperationException("Journal entry service is not available");
        }

        var count = 3;
        if (parameters.TryGetValue("count", out var countObj) && countObj is not null)
        {
            count = AgentToolParameterParser.ParseInt(countObj, "count", 1, 10);
        }

        var entries = await context.JournalEntryService.GetRecentForUser(context.UserId, count);
        return new
        {
            success = true,
            entries = entries.Select(e => new
            {
                id = e.Id,
                summary = e.Summary ?? Truncate(e.Content, 200),
                createdAt = e.CreatedAt
            })
        };
    }

    private static string Truncate(string? text, int max)
        => string.IsNullOrEmpty(text) || text.Length <= max ? text ?? string.Empty : text[..max] + "...";
}

public sealed class GetJournalStreakToolHandler : IAgentToolHandler
{
    public string Name => "get_journal_streak";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Get the user's current and longest journal writing streak.",
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
        if (context.JournalStreakService is null)
        {
            throw new InvalidOperationException("Journal streak service is not available");
        }

        string? timezoneId = null;
        if (context.UserProfileService is not null)
        {
            var profile = await context.UserProfileService.GetByUserId(context.UserId);
            timezoneId = TryGetTimezoneId(profile?.Preference?.Preferences);
        }

        var streak = await context.JournalStreakService.GetStreakSummary(context.UserId, timezoneId, cancellationToken);
        return new
        {
            success = true,
            currentStreak = streak.CurrentStreak,
            longestStreak = streak.LongestStreak
        };
    }

    private static string? TryGetTimezoneId(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("timezone", out var tzElement) &&
                tzElement.ValueKind == JsonValueKind.String)
            {
                return tzElement.GetString();
            }
        }
        catch
        {
            // Ignore invalid JSON
        }

        return null;
    }
}
