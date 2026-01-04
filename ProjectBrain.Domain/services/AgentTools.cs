namespace ProjectBrain.Domain;

using ProjectBrain.Database.Models;
using System.Text.Json;

/// <summary>
/// Tool definitions and execution handlers for the AI agent
/// </summary>
public static class AgentTools
{
    /// <summary>
    /// Executes the create_daily_goals tool
    /// </summary>
    public static async Task<object> ExecuteCreateDailyGoals(
        IGoalService goalService,
        string userId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("goals", out var goalsObj) || goalsObj == null)
        {
            throw new ArgumentException("goals parameter is required");
        }

        var goalsList = new List<string>();
        if (goalsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    goalsList.Add(item.GetString() ?? string.Empty);
                }
            }
        }
        else if (goalsObj is IEnumerable<object> enumerable)
        {
            goalsList = enumerable.Select(g => g?.ToString() ?? string.Empty).ToList();
        }
        else
        {
            throw new ArgumentException("goals must be an array of strings");
        }

        if (goalsList.Count == 0 || goalsList.Count > 3)
        {
            throw new ArgumentException("Goals must contain between 1 and 3 items");
        }

        var goals = await goalService.CreateOrUpdateGoalsAsync(userId, goalsList, cancellationToken);
        
        return new
        {
            success = true,
            message = $"Successfully created {goalsList.Count} goal(s) for today",
            goals = goals.Select(g => new { index = g.Index, message = g.Message, completed = g.Completed })
        };
    }

    /// <summary>
    /// Executes the get_todays_goals tool
    /// </summary>
    public static async Task<object> ExecuteGetTodaysGoals(
        IGoalService goalService,
        string userId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var goals = await goalService.GetTodaysGoalsAsync(userId, cancellationToken);
        
        return new
        {
            success = true,
            goals = goals.Select(g => new { index = g.Index, message = g.Message, completed = g.Completed, completedAt = g.CompletedAt })
        };
    }

    /// <summary>
    /// Executes the complete_goal tool
    /// </summary>
    public static async Task<object> ExecuteCompleteGoal(
        IGoalService goalService,
        string userId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!parameters.TryGetValue("index", out var indexObj) || indexObj == null)
        {
            throw new ArgumentException("index parameter is required");
        }

        if (!parameters.TryGetValue("completed", out var completedObj) || completedObj == null)
        {
            throw new ArgumentException("completed parameter is required");
        }

        int index;
        if (indexObj is JsonElement jsonIndex && jsonIndex.ValueKind == JsonValueKind.Number)
        {
            index = jsonIndex.GetInt32();
        }
        else if (indexObj is int intIndex)
        {
            index = intIndex;
        }
        else
        {
            throw new ArgumentException("index must be an integer");
        }

        bool completed;
        if (completedObj is JsonElement jsonCompleted)
        {
            completed = jsonCompleted.ValueKind == JsonValueKind.True;
        }
        else if (completedObj is bool boolCompleted)
        {
            completed = boolCompleted;
        }
        else
        {
            throw new ArgumentException("completed must be a boolean");
        }

        if (index < 0 || index > 2)
        {
            throw new ArgumentException("index must be between 0 and 2");
        }

        var goals = await goalService.CompleteGoalAsync(userId, index, completed, cancellationToken);
        
        return new
        {
            success = true,
            message = $"Goal at index {index} marked as {(completed ? "completed" : "incomplete")}",
            goals = goals.Select(g => new { index = g.Index, message = g.Message, completed = g.Completed })
        };
    }

    /// <summary>
    /// Gets the tool definition for OpenAI function calling
    /// </summary>
    public static Dictionary<string, object> GetToolDefinition(string toolName)
    {
        return toolName switch
        {
            "create_daily_goals" => new Dictionary<string, object>
            {
                ["type"] = "function",
                ["function"] = new Dictionary<string, object>
                {
                    ["name"] = "create_daily_goals",
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
            },
            "get_todays_goals" => new Dictionary<string, object>
            {
                ["type"] = "function",
                ["function"] = new Dictionary<string, object>
                {
                    ["name"] = "get_todays_goals",
                    ["description"] = "Retrieve today's daily goals (eggs) for the user",
                    ["parameters"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>(),
                        ["required"] = Array.Empty<string>()
                    }
                }
            },
            "complete_goal" => new Dictionary<string, object>
            {
                ["type"] = "function",
                ["function"] = new Dictionary<string, object>
                {
                    ["name"] = "complete_goal",
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
            },
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }

    /// <summary>
    /// Gets all available tool definitions
    /// </summary>
    public static List<Dictionary<string, object>> GetAllToolDefinitions()
    {
        return new List<Dictionary<string, object>>
        {
            GetToolDefinition("create_daily_goals"),
            GetToolDefinition("get_todays_goals"),
            GetToolDefinition("complete_goal")
        };
    }

    /// <summary>
    /// Executes a tool by name
    /// </summary>
    public static async Task<object> ExecuteTool(
        string toolName,
        IGoalService goalService,
        string userId,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        return toolName switch
        {
            "create_daily_goals" => await ExecuteCreateDailyGoals(goalService, userId, parameters, cancellationToken),
            "get_todays_goals" => await ExecuteGetTodaysGoals(goalService, userId, parameters, cancellationToken),
            "complete_goal" => await ExecuteCompleteGoal(goalService, userId, parameters, cancellationToken),
            _ => throw new ArgumentException($"Unknown tool: {toolName}")
        };
    }
}

