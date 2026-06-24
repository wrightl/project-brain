namespace ProjectBrain.Domain;

public interface IAgentToolHandler
{
    string Name { get; }
    bool RequiresConfirmation => false;
    bool PausesTurn => false;
    string? BuildConfirmationPreview(Dictionary<string, object> parameters) => null;
    Task<bool> IsEnabledAsync(AgentToolContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    Dictionary<string, object> GetDefinition();
    Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

public interface IAgentToolRegistry
{
    IReadOnlyList<Dictionary<string, object>> GetAllDefinitions();
    Task<IReadOnlyList<Dictionary<string, object>>> GetEnabledDefinitionsAsync(
        AgentToolContext context,
        CancellationToken cancellationToken = default);
    IAgentToolHandler? TryGetHandler(string toolName);
    Task<object> ExecuteAsync(string toolName, AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

public sealed class AgentToolRegistry : IAgentToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAgentToolHandler> _handlers;

    public AgentToolRegistry(IEnumerable<IAgentToolHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<Dictionary<string, object>> GetAllDefinitions()
    {
        return _handlers.Values.Select(h => h.GetDefinition()).ToList();
    }

    public async Task<IReadOnlyList<Dictionary<string, object>>> GetEnabledDefinitionsAsync(
        AgentToolContext context,
        CancellationToken cancellationToken = default)
    {
        var definitions = new List<Dictionary<string, object>>();
        foreach (var handler in _handlers.Values)
        {
            if (await handler.IsEnabledAsync(context, cancellationToken))
            {
                definitions.Add(handler.GetDefinition());
            }
        }

        return definitions;
    }

    public IAgentToolHandler? TryGetHandler(string toolName)
    {
        return _handlers.TryGetValue(toolName, out var handler) ? handler : null;
    }

    public Task<object> ExecuteAsync(
        string toolName,
        AgentToolContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(toolName, out var handler))
        {
            throw new ArgumentException($"Unknown tool: {toolName}");
        }

        return handler.ExecuteAsync(context, parameters, cancellationToken);
    }
}
