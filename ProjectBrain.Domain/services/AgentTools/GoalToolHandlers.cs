namespace ProjectBrain.Domain.AgentTools;

using System.Text.Json;
using ProjectBrain.Database.Models;

public static class AgentToolParameterParser
{
    public static List<string> ParseStringArray(object? value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentException($"{parameterName} parameter is required");
        }

        var goalsList = new List<string>();
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    goalsList.Add(item.GetString() ?? string.Empty);
                }
            }
        }
        else if (value is IEnumerable<object> enumerable)
        {
            goalsList = enumerable.Select(g => g?.ToString() ?? string.Empty).ToList();
        }
        else
        {
            throw new ArgumentException($"{parameterName} must be an array of strings");
        }

        return goalsList;
    }

    public static int ParseInt(object? value, string parameterName, int? min = null, int? max = null)
    {
        if (value == null)
        {
            throw new ArgumentException($"{parameterName} parameter is required");
        }

        int result;
        if (value is JsonElement json && json.ValueKind == JsonValueKind.Number)
        {
            result = json.GetInt32();
        }
        else if (value is int intValue)
        {
            result = intValue;
        }
        else
        {
            throw new ArgumentException($"{parameterName} must be an integer");
        }

        if (min.HasValue && result < min.Value)
        {
            throw new ArgumentException($"{parameterName} must be at least {min.Value}");
        }

        if (max.HasValue && result > max.Value)
        {
            throw new ArgumentException($"{parameterName} must be at most {max.Value}");
        }

        return result;
    }

    public static bool ParseBool(object? value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentException($"{parameterName} parameter is required");
        }

        if (value is JsonElement json)
        {
            return json.ValueKind == JsonValueKind.True;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        throw new ArgumentException($"{parameterName} must be a boolean");
    }

    public static string ParseString(object? value, string parameterName, bool required = true)
    {
        if (value == null)
        {
            if (!required)
            {
                return string.Empty;
            }

            throw new ArgumentException($"{parameterName} parameter is required");
        }

        if (value is JsonElement json && json.ValueKind == JsonValueKind.String)
        {
            return json.GetString() ?? string.Empty;
        }

        var text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException($"{parameterName} parameter is required");
        }

        return text;
    }

    public static Guid ParseGuid(object? value, string parameterName)
    {
        var text = ParseString(value, parameterName);
        if (!Guid.TryParse(text, out var id))
        {
            throw new ArgumentException($"{parameterName} must be a valid GUID");
        }

        return id;
    }

    public static List<MultidayGoalPlan> ParseDayGoalPlans(object? value, string parameterName, int maxDays = 7)
    {
        if (value is null)
        {
            throw new ArgumentException($"{parameterName} parameter is required");
        }

        JsonElement arrayElement;
        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException($"{parameterName} must be an array of day plans");
            }

            arrayElement = jsonElement;
        }
        else
        {
            var serialized = JsonSerializer.Serialize(value);
            using var doc = JsonDocument.Parse(serialized);
            arrayElement = doc.RootElement.Clone();
            if (arrayElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException($"{parameterName} must be an array of day plans");
            }
        }

        var plans = new List<MultidayGoalPlan>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (!item.TryGetProperty("date", out var dateProperty))
            {
                throw new ArgumentException("Each day plan must include a date (YYYY-MM-DD)");
            }

            var dateText = dateProperty.GetString();
            if (string.IsNullOrWhiteSpace(dateText) || !DateOnly.TryParse(dateText, out var date))
            {
                throw new ArgumentException("Each day plan date must be a valid YYYY-MM-DD value");
            }

            if (!item.TryGetProperty("goals", out var goalsProperty))
            {
                throw new ArgumentException("Each day plan must include a goals array");
            }

            var goals = ParseStringArray(goalsProperty, "goals");
            plans.Add(new MultidayGoalPlan { Date = date, Goals = goals });
        }

        if (plans.Count == 0)
        {
            throw new ArgumentException($"{parameterName} must contain at least one day plan");
        }

        if (plans.Count > maxDays)
        {
            throw new ArgumentException($"{parameterName} cannot contain more than {maxDays} day plans");
        }

        return plans;
    }
}

public sealed class CreateDailyGoalsToolHandler : IAgentToolHandler
{
    public string Name => "create_daily_goals";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Create or update today's daily goals (eggs). You can create 1-3 goals. This will replace any existing goals for today.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["goals"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["items"] = new Dictionary<string, object> { ["type"] = "string" },
                        ["minItems"] = 1,
                        ["maxItems"] = 3,
                        ["description"] = "Array of 1-3 goal strings to create for today"
                    }
                },
                ["required"] = new[] { "goals" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("goals", out var goalsObj))
        {
            throw new ArgumentException("goals parameter is required");
        }

        var goalsList = AgentToolParameterParser.ParseStringArray(goalsObj, "goals");
        if (goalsList.Count == 0 || goalsList.Count > 3)
        {
            throw new ArgumentException("Goals must contain between 1 and 3 items");
        }

        var goals = await context.GoalService.CreateOrUpdateGoalsAsync(context.UserId, goalsList, cancellationToken);
        await context.GoalMutationSideEffects.NotifyGoalsChangedAsync(context.UserId, cancellationToken);

        return new
        {
            success = true,
            message = $"Successfully created {goalsList.Count} goal(s) for today",
            goals = goals.Select(g => new { index = g.Index, message = g.Message, completed = g.Completed })
        };
    }
}

public sealed class CreateGoalsForDaysToolHandler : IAgentToolHandler
{
    public string Name => "create_goals_for_days";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Create or update daily goals (eggs) for multiple future dates. Each day can have 1-3 goals. Replaces existing goals on those dates. Use for planning up to 7 days ahead.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["dayPlans"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["minItems"] = 1,
                        ["maxItems"] = 7,
                        ["description"] = "List of per-day goal plans",
                        ["items"] = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["date"] = new Dictionary<string, object>
                                {
                                    ["type"] = "string",
                                    ["description"] = "Date in YYYY-MM-DD format (today or up to 30 days ahead)"
                                },
                                ["goals"] = new Dictionary<string, object>
                                {
                                    ["type"] = "array",
                                    ["items"] = new Dictionary<string, object> { ["type"] = "string" },
                                    ["minItems"] = 1,
                                    ["maxItems"] = 3,
                                    ["description"] = "1-3 goal strings for this date"
                                }
                            },
                            ["required"] = new[] { "date", "goals" }
                        }
                    }
                },
                ["required"] = new[] { "dayPlans" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("dayPlans", out var dayPlansObj))
        {
            throw new ArgumentException("dayPlans parameter is required");
        }

        var dayPlans = AgentToolParameterParser.ParseDayGoalPlans(
            dayPlansObj,
            "dayPlans",
            GoalService.MaxMultidayPlans);

        var results = await context.GoalService.CreateOrUpdateGoalsForDatesAsync(
            context.UserId,
            dayPlans,
            cancellationToken);

        await context.GoalMutationSideEffects.NotifyGoalsChangedAsync(context.UserId, cancellationToken);

        return new
        {
            success = true,
            message = $"Successfully created goals for {results.Count} day(s)",
            days = results.Select(r => new
            {
                date = r.Date.ToString("yyyy-MM-dd"),
                goals = r.Goals.Select(g => new { index = g.Index, message = g.Message, completed = g.Completed })
            })
        };
    }
}

public sealed class GetTodaysGoalsToolHandler : IAgentToolHandler
{
    public string Name => "get_todays_goals";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Retrieve today's daily goals (eggs) for the user",
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
        var goals = await context.GoalService.GetTodaysGoalsAsync(context.UserId, cancellationToken);
        return new
        {
            success = true,
            goals = goals.Select(g => new { index = g.Index, message = g.Message, completed = g.Completed, completedAt = g.CompletedAt })
        };
    }
}

public sealed class CompleteGoalToolHandler : IAgentToolHandler
{
    public string Name => "complete_goal";

    public Dictionary<string, object> GetDefinition() => new()
    {
        ["type"] = "function",
        ["function"] = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = "Mark a goal as complete or incomplete. Goals are indexed 0, 1, or 2.",
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["index"] = new Dictionary<string, object>
                    {
                        ["type"] = "integer",
                        ["minimum"] = 0,
                        ["maximum"] = 2,
                        ["description"] = "The index of the goal (0, 1, or 2)"
                    },
                    ["completed"] = new Dictionary<string, object>
                    {
                        ["type"] = "boolean",
                        ["description"] = "Whether the goal is completed (true) or not (false)"
                    }
                },
                ["required"] = new[] { "index", "completed" }
            }
        }
    };

    public async Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var index = AgentToolParameterParser.ParseInt(parameters.GetValueOrDefault("index"), "index", 0, 2);
        var completed = AgentToolParameterParser.ParseBool(parameters.GetValueOrDefault("completed"), "completed");

        var goals = await context.GoalService.CompleteGoalAsync(context.UserId, index, completed, cancellationToken);
        await context.GoalMutationSideEffects.NotifyGoalsChangedAsync(context.UserId, cancellationToken);

        return new
        {
            success = true,
            message = $"Goal at index {index} marked as {(completed ? "completed" : "incomplete")}",
            goals = goals.Select(g => new { index = g.Index, message = g.Message, completed = g.Completed })
        };
    }
}
