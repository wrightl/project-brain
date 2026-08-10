namespace ProjectBrain.Domain.AgentTools;

using ProjectBrain.Database.Models;
using ProjectBrain.Domain.Dtos;

public sealed class SuggestCopingStrategiesToolHandler : IAgentToolHandler
{
    public string Name => "suggest_coping_strategies";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Suggest coping strategies based on the user's current situation. Returns up to 3 strategies for the user to consider.",
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
        if (context.StrategySuggestionService is null)
        {
            throw new InvalidOperationException("Strategy suggestion service is not available");
        }

        var query = string.IsNullOrWhiteSpace(context.LastUserQuery)
            ? "Suggest coping strategies for my current situation"
            : context.LastUserQuery;

        var suggestions = await context.StrategySuggestionService.GetSuggestionsAsync(
            query,
            context.UserId,
            string.Empty,
            "User",
            new List<AgentChatMessage>(),
            new ChatMemoryContext(),
            context.ConversationId,
            cancellationToken: cancellationToken);

        return new
        {
            success = true,
            strategies = suggestions.Select(s => new { title = s.Title, description = s.Description, iconKey = s.IconKey })
        };
    }
}

public sealed class SaveCopingStrategyToolHandler : IAgentToolHandler
{
    public string Name => "save_coping_strategy";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Save a coping strategy to the user's library.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["title"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Strategy title" },
                    ["description"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Strategy description" },
                    ["iconKey"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional icon key" }
                },
                ["required"] = new[] { "title", "description" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.CopingStrategyService is null || context.CopingStrategySideEffects is null)
        {
            throw new InvalidOperationException("Coping strategy services are not available");
        }

        var title = AgentToolParameterParser.ParseString(parameters.GetValueOrDefault("title"), "title");
        var description = AgentToolParameterParser.ParseString(parameters.GetValueOrDefault("description"), "description");
        var iconKey = parameters.TryGetValue("iconKey", out var icon) ? icon?.ToString() : null;

        var created = await context.CopingStrategyService.CreateAsync(
            context.UserId, title, description, iconKey, cancellationToken);

        await context.CopingStrategySideEffects.OnStrategyCreatedAsync(
            context.UserId,
            created.Id,
            created.Title,
            created.Description,
            created.IconKey,
            created.Rating,
            created.SavedAt,
            context.ConversationId,
            cancellationToken);

        return new
        {
            success = true,
            message = $"Saved coping strategy '{title}' to your library",
            strategy = new { id = created.Id, title = created.Title, description = created.Description }
        };
    }
}

public sealed class GetCopingStrategiesToolHandler : IAgentToolHandler
{
    public string Name => "get_coping_strategies";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Retrieve the user's saved coping strategies library.",
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
        if (context.CopingStrategyService is null)
        {
            throw new InvalidOperationException("Coping strategy service is not available");
        }

        var library = await context.CopingStrategyService.GetLibraryAsync(context.UserId, cancellationToken);
        return new
        {
            success = true,
            strategies = library.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                description = s.Description,
                iconKey = s.IconKey,
                rating = s.Rating
            })
        };
    }
}

public sealed class RateCopingStrategyToolHandler : IAgentToolHandler
{
    public string Name => "rate_coping_strategy";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Rate a saved coping strategy from 1 to 5.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["strategyId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Strategy ID (GUID)" },
                    ["rating"] = new Dictionary<string, object> { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 5 }
                },
                ["required"] = new[] { "strategyId", "rating" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.CopingStrategyService is null || context.CopingStrategySideEffects is null)
        {
            throw new InvalidOperationException("Coping strategy services are not available");
        }

        var strategyId = AgentToolParameterParser.ParseGuid(parameters.GetValueOrDefault("strategyId"), "strategyId");
        var rating = AgentToolParameterParser.ParseInt(parameters.GetValueOrDefault("rating"), "rating", 1, 5);

        var updated = await context.CopingStrategyService.UpdateRatingAsync(
            context.UserId, strategyId, rating, cancellationToken);

        if (updated is null)
        {
            return new { success = false, message = "Strategy not found" };
        }

        await context.CopingStrategySideEffects.EnqueueStrategyReindexAsync(
            context.UserId,
            updated.Id,
            updated.Title,
            updated.Description,
            updated.IconKey,
            updated.Rating,
            updated.SavedAt,
            cancellationToken);

        return new { success = true, message = $"Rated strategy {rating}/5", strategyId = updated.Id, rating };
    }
}

public sealed class UploadKnowledgeDocumentToolHandler : IAgentToolHandler
{
    public string Name => "upload_knowledge_document";

    public Task<bool> IsEnabledAsync(AgentToolContext context, CancellationToken cancellationToken = default) =>
        AgentToolGating.IsFileUploadEnabledAsync(context, cancellationToken);

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Create and upload a markdown knowledge document to the user's personal knowledge library.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["filename"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Filename ending in .md" },
                    ["markdown"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Markdown content" }
                },
                ["required"] = new[] { "filename", "markdown" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.KnowledgeUploadService is null)
        {
            throw new InvalidOperationException("Knowledge upload service is not available");
        }

        var filename = AgentToolParameterParser.ParseString(parameters.GetValueOrDefault("filename"), "filename");
        var markdown = AgentToolParameterParser.ParseString(parameters.GetValueOrDefault("markdown"), "markdown");

        var result = await context.KnowledgeUploadService.UploadMarkdownAsync(
            context.UserId, filename, markdown, cancellationToken);

        return new
        {
            success = result.Success,
            message = result.Message,
            resourceId = result.ResourceId,
            filename = result.Filename
        };
    }
}

public sealed class ListKnowledgeResourcesToolHandler : IAgentToolHandler
{
    public string Name => "list_knowledge_resources";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "List the user's uploaded knowledge resources.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["limit"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "Max items (default 20)" }
                },
                ["required"] = Array.Empty<string>()
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.KnowledgeUploadService is null)
        {
            throw new InvalidOperationException("Knowledge upload service is not available");
        }

        var limit = 20;
        if (parameters.TryGetValue("limit", out var limitObj) && limitObj is not null)
        {
            limit = AgentToolParameterParser.ParseInt(limitObj, "limit", 1, 100);
        }

        var resources = await context.KnowledgeUploadService.ListResourcesAsync(context.UserId, limit, cancellationToken);
        return new { success = true, resources };
    }
}

public sealed class DeleteKnowledgeResourceToolHandler : IAgentToolHandler
{
    public string Name => "delete_knowledge_resource";
    public bool RequiresConfirmation => true;

    public Task<bool> IsEnabledAsync(AgentToolContext context, CancellationToken cancellationToken = default) =>
        AgentToolGating.IsFileUploadEnabledAsync(context, cancellationToken);

    public string? BuildConfirmationPreview(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("resourceId", out var resourceId) && resourceId is not null)
        {
            return $"Delete knowledge resource {resourceId}";
        }

        return "Delete knowledge resource";
    }

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Delete a knowledge resource. Ask for confirmation before calling.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["resourceId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Resource ID (GUID)" }
                },
                ["required"] = new[] { "resourceId" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.KnowledgeUploadService is null)
        {
            throw new InvalidOperationException("Knowledge upload service is not available");
        }

        var resourceId = AgentToolParameterParser.ParseGuid(parameters.GetValueOrDefault("resourceId"), "resourceId");
        var deleted = await context.KnowledgeUploadService.DeleteResourceAsync(context.UserId, resourceId, cancellationToken);
        return new { success = deleted, message = deleted ? "Resource deleted" : "Resource not found" };
    }
}

public sealed class SearchCoachesToolHandler : IAgentToolHandler
{
    public string Name => "search_coaches";

    public Task<bool> IsEnabledAsync(AgentToolContext context, CancellationToken cancellationToken = default) =>
        AgentToolGating.IsCoachFeatureEnabledAsync(context, cancellationToken);

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Search for coaches by location, specialisms, or age groups.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["city"] = new Dictionary<string, object> { ["type"] = "string" },
                    ["stateProvince"] = new Dictionary<string, object> { ["type"] = "string" },
                    ["country"] = new Dictionary<string, object> { ["type"] = "string" },
                    ["useMyLocation"] = new Dictionary<string, object> { ["type"] = "boolean" },
                    ["distanceMiles"] = new Dictionary<string, object> { ["type"] = "number" },
                    ["specialisms"] = new Dictionary<string, object> { ["type"] = "array", ["items"] = new Dictionary<string, object> { ["type"] = "string" } },
                    ["ageGroups"] = new Dictionary<string, object> { ["type"] = "array", ["items"] = new Dictionary<string, object> { ["type"] = "string" } }
                },
                ["required"] = Array.Empty<string>()
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (context.CoachProfileService is null)
        {
            throw new InvalidOperationException("Coach profile service is not available");
        }

        var specialisms = ParseOptionalStringArray(parameters, "specialisms");
        var ageGroups = ParseOptionalStringArray(parameters, "ageGroups");
        var useMyLocation = parameters.TryGetValue("useMyLocation", out var loc)
            && AgentToolParameterParser.ParseBool(loc, "useMyLocation");

        List<CoachProfile> coaches;
        if (useMyLocation && context.UserService is not null)
        {
            var user = await context.UserService.GetById(context.UserId);
            var lat = user?.Latitude;
            var lng = user?.Longitude;
            var radius = parameters.TryGetValue("distanceMiles", out var dist)
                ? Convert.ToDouble(dist.ToString())
                : 25.0;

            if (lat is null || lng is null)
            {
                return new { success = false, message = "Your profile does not have a location set for nearby search." };
            }

            coaches = await context.CoachProfileService.SearchByDistance(lat.Value, lng.Value, radius, ageGroups, specialisms);
        }
        else
        {
            coaches = await context.CoachProfileService.Search(
                parameters.GetValueOrDefault("city")?.ToString(),
                parameters.GetValueOrDefault("stateProvince")?.ToString(),
                parameters.GetValueOrDefault("country")?.ToString(),
                ageGroups,
                specialisms);
        }

        var results = new List<object>();
        foreach (var coach in coaches.Take(10))
        {
            var coachUserId = coach.UserId;
            var (connectionStatus, connectionId) = await CoachAgentConnectionHelper.GetConnectionMetadataAsync(
                context,
                coachUserId,
                cancellationToken);

            results.Add(new
            {
                coachProfileId = coach.Id,
                name = coach.User?.FullName ?? "Coach",
                bio = Truncate(coach.Bio, 200),
                specialisms = coach.Specialisms?.Select(s => s.Specialism).ToList() ?? new List<string>(),
                connectionStatus,
                connectionId
            });
        }

        return new { success = true, coaches = results, count = results.Count };
    }

    private static List<string>? ParseOptionalStringArray(Dictionary<string, object> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return AgentToolParameterParser.ParseStringArray(value, key);
    }

    private static string? Truncate(string? text, int max)
        => string.IsNullOrEmpty(text) || text.Length <= max ? text : text[..max] + "...";
}

public sealed class GetConnectedCoachesToolHandler : IAgentToolHandler
{
    public string Name => "get_connected_coaches";

    public Task<bool> IsEnabledAsync(AgentToolContext context, CancellationToken cancellationToken = default) =>
        AgentToolGating.IsCoachFeatureEnabledAsync(context, cancellationToken);

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "List coaches the user is already connected with.",
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
        if (context.ConnectionService is null || context.CoachProfileService is null)
        {
            throw new InvalidOperationException("Coach connection services are not available");
        }

        var connected = await context.ConnectionService.GetConnectedCoachIdsAsync(context.UserId);
        var coaches = new List<object>();

        foreach (var connection in connected)
        {
            var profile = await context.CoachProfileService.GetByUserId(connection.CoachId);
            if (profile?.User is null)
            {
                continue;
            }

            var connectionStatus = CoachAgentConnectionHelper.MapConnectionStatus(connection.Status);
            coaches.Add(new
            {
                coachProfileId = profile.Id,
                name = profile.User.FullName,
                connectionStatus,
                connectionId = connection.Id
            });
        }

        return new { success = true, coaches };
    }
}

internal static class CoachAgentConnectionHelper
{
    public static async Task<(string ConnectionStatus, string? ConnectionId)> GetConnectionMetadataAsync(
        AgentToolContext context,
        string coachUserId,
        CancellationToken cancellationToken = default)
    {
        if (context.ConnectionService is null || string.IsNullOrWhiteSpace(coachUserId))
        {
            return ("none", null);
        }

        var connection = await context.ConnectionService.GetConnectionAsync(context.UserId, coachUserId);
        if (connection is null || connection.Status is "cancelled" or "rejected")
        {
            return ("none", null);
        }

        return (MapConnectionStatus(connection.Status), connection.Id.ToString());
    }

    public static string MapConnectionStatus(string status) =>
        status switch
        {
            "pending" => "pending",
            "accepted" => "connected",
            _ => "none"
        };
}
