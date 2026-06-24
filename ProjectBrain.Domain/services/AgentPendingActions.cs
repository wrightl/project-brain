namespace ProjectBrain.Domain;

using System.Text.Json;

public sealed class AgentPendingAction
{
    public Guid Id { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Preview { get; set; }
    public string Status { get; set; } = "awaiting_confirmation";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class AgentPendingActionStore
{
    private const string PendingActionsKey = "pendingActions";

    public static IReadOnlyList<AgentPendingAction> GetPendingActions(AgentWorkflowState workflowState)
    {
        if (!workflowState.CurrentState.TryGetValue(PendingActionsKey, out var raw) || raw is null)
        {
            return Array.Empty<AgentPendingAction>();
        }

        return DeserializeList(raw);
    }

    public static AgentPendingAction? Find(AgentWorkflowState workflowState, Guid actionId)
    {
        return GetPendingActions(workflowState)
            .FirstOrDefault(a => a.Id == actionId && a.Status == "awaiting_confirmation");
    }

    public static void Add(AgentWorkflowState workflowState, AgentPendingAction action)
    {
        var list = GetPendingActions(workflowState).ToList();
        list.Add(action);
        workflowState.CurrentState[PendingActionsKey] = SerializeList(list);
    }

    public static bool Remove(AgentWorkflowState workflowState, Guid actionId)
    {
        var list = GetPendingActions(workflowState).ToList();
        var removed = list.RemoveAll(a => a.Id == actionId) > 0;
        if (removed)
        {
            workflowState.CurrentState[PendingActionsKey] = SerializeList(list);
        }

        return removed;
    }

    public static void MarkCancelled(AgentWorkflowState workflowState, Guid actionId)
    {
        var list = GetPendingActions(workflowState).ToList();
        var action = list.FirstOrDefault(a => a.Id == actionId);
        if (action is not null)
        {
            action.Status = "cancelled";
            workflowState.CurrentState[PendingActionsKey] = SerializeList(list);
        }
    }

    private static string SerializeList(IReadOnlyList<AgentPendingAction> actions)
    {
        return JsonSerializer.Serialize(actions);
    }

    private static List<AgentPendingAction> DeserializeList(object raw)
    {
        if (raw is string json)
        {
            return JsonSerializer.Deserialize<List<AgentPendingAction>>(json) ?? new List<AgentPendingAction>();
        }

        var serialized = JsonSerializer.Serialize(raw);
        return JsonSerializer.Deserialize<List<AgentPendingAction>>(serialized) ?? new List<AgentPendingAction>();
    }
}
