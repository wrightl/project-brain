namespace ProjectBrain.Api.Services;

using System.Text.Json;

public static class AgentActionCardMapper
{
    public static IEnumerable<object> MapToolResult(string toolName, object? result, bool success)
    {
        if (!success || result is null)
        {
            yield break;
        }

        if (result is JsonElement element)
        {
            foreach (var card in MapJsonElement(toolName, element))
            {
                yield return card;
            }

            yield break;
        }

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        foreach (var card in MapJsonElement(toolName, doc.RootElement))
        {
            yield return card;
        }
    }

    private static IEnumerable<object> MapJsonElement(string toolName, JsonElement root)
    {
        switch (toolName)
        {
            case "create_daily_goals":
                if (root.TryGetProperty("goals", out var goals) && goals.ValueKind == JsonValueKind.Array)
                {
                    yield return new
                    {
                        cardType = "goals_created",
                        goals = goals.EnumerateArray().Select(g => new
                        {
                            index = g.TryGetProperty("index", out var idx) ? idx.GetInt32() : 0,
                            message = g.TryGetProperty("message", out var msg) ? msg.GetString() : null,
                            completed = g.TryGetProperty("completed", out var comp) && comp.GetBoolean()
                        }),
                        href = "/app/user/eggs",
                        label = "View your goals"
                    };
                }

                break;

            case "create_goals_for_days":
                if (root.TryGetProperty("days", out var days) && days.ValueKind == JsonValueKind.Array)
                {
                    yield return new
                    {
                        cardType = "goals_created",
                        days = days.EnumerateArray().Select(d => new
                        {
                            date = d.TryGetProperty("date", out var date) ? date.GetString() : null,
                            goalCount = d.TryGetProperty("goals", out var dayGoals) && dayGoals.ValueKind == JsonValueKind.Array
                                ? dayGoals.GetArrayLength()
                                : 0
                        }),
                        href = "/app/user/eggs",
                        label = "View your goals"
                    };
                }

                break;

            case "save_coping_strategy":
                if (root.TryGetProperty("strategy", out var strategy))
                {
                    yield return new
                    {
                        cardType = "strategy_saved",
                        title = strategy.TryGetProperty("title", out var title) ? title.GetString() : null,
                        description = strategy.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        href = "/app/user/strategies",
                        label = "View your strategies"
                    };
                }

                break;

            case "search_coaches":
            case "get_connected_coaches":
                if (root.TryGetProperty("coaches", out var coaches) && coaches.ValueKind == JsonValueKind.Array)
                {
                    yield return new
                    {
                        cardType = "coaches_found",
                        coaches = coaches.EnumerateArray().Select(c => new
                        {
                            coachProfileId = c.TryGetProperty("coachProfileId", out var id) ? GetJsonString(id) : null,
                            name = c.TryGetProperty("name", out var name) ? GetJsonString(name) : null,
                            bio = c.TryGetProperty("bio", out var bio) ? GetJsonString(bio) : null,
                            connectionStatus = c.TryGetProperty("connectionStatus", out var status) ? GetJsonString(status) : null,
                            connectionId = c.TryGetProperty("connectionId", out var connId) ? GetJsonString(connId) : null
                        }),
                        href = "/app/user/find-coaches",
                        label = "Browse coaches"
                    };
                }

                break;

            case "upload_knowledge_document":
                yield return new
                {
                    cardType = "document_uploaded",
                    filename = root.TryGetProperty("filename", out var filename) ? filename.GetString() : null,
                    resourceId = root.TryGetProperty("resourceId", out var resourceId) ? resourceId.GetString() : null,
                    href = "/app/user/manage-resources",
                    label = "Manage resources"
                };
                break;

            case "suggest_daily_goals":
                if (root.TryGetProperty("goals", out var suggestedGoals) && suggestedGoals.ValueKind == JsonValueKind.Array)
                {
                    yield return new
                    {
                        cardType = "goals_suggested",
                        goals = suggestedGoals.EnumerateArray().Select(g => new
                        {
                            message = g.ValueKind == JsonValueKind.String ? g.GetString() : g.GetRawText()
                        }),
                        href = "/app/user/eggs",
                        label = "View your goals"
                    };
                }

                break;

            case "get_goal_streak":
                if (root.TryGetProperty("currentStreak", out var currentStreak))
                {
                    yield return new
                    {
                        cardType = "goal_streak",
                        currentStreak = currentStreak.GetInt32(),
                        longestStreak = root.TryGetProperty("longestStreak", out var longest) ? longest.GetInt32() : 0,
                        href = "/app/user/eggs",
                        label = "View your goals"
                    };
                }

                break;

            case "create_journal_entry":
                if (root.TryGetProperty("entry", out var entry))
                {
                    var entryId = entry.TryGetProperty("id", out var idEl) ? GetJsonString(idEl) : null;
                    yield return new
                    {
                        cardType = "journal_entry_created",
                        entryId,
                        summary = entry.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
                        href = entryId is not null ? $"/app/user/journal/{entryId}" : "/app/user/journal",
                        label = "View journal entry"
                    };
                }

                break;

            case "remember_fact":
                if (root.TryGetProperty("fact", out var fact))
                {
                    yield return new
                    {
                        cardType = "memory_saved",
                        title = fact.TryGetProperty("content", out var content) ? content.GetString() : null,
                        description = fact.TryGetProperty("category", out var category) ? category.GetString() : null,
                        href = "/app/user/profile",
                        label = "View learned memories"
                    };
                }

                break;

            case "forget_memory":
                yield return new
                {
                    cardType = "memory_deleted",
                    message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Memory forgotten",
                    href = "/app/user/profile",
                    label = "View learned memories"
                };
                break;

            case "delete_knowledge_resource":
                yield return new
                {
                    cardType = "document_deleted",
                    message = root.TryGetProperty("message", out var deleteMsg) ? deleteMsg.GetString() : "Resource deleted",
                    href = "/app/user/manage-resources",
                    label = "Manage resources"
                };
                break;
        }
    }

    private static string? GetJsonString(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            _ => null
        };
}
