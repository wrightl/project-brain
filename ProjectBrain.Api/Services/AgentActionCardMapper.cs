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
                if (root.TryGetProperty("coaches", out var coaches) && coaches.ValueKind == JsonValueKind.Array)
                {
                    yield return new
                    {
                        cardType = "coaches_found",
                        coaches = coaches.EnumerateArray().Select(c => new
                        {
                            coachProfileId = c.TryGetProperty("coachProfileId", out var id) ? id.GetString() : null,
                            name = c.TryGetProperty("name", out var name) ? name.GetString() : null,
                            bio = c.TryGetProperty("bio", out var bio) ? bio.GetString() : null,
                            profileUrl = c.TryGetProperty("profileUrl", out var url) ? url.GetString() : null
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
        }
    }
}
