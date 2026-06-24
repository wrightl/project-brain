namespace ProjectBrain.Domain;

public interface IAgentToolHandler
{
    string Name { get; }
    Dictionary<string, object> GetDefinition();
    Task<object> ExecuteAsync(AgentToolContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

public interface IAgentToolRegistry
{
    IReadOnlyList<Dictionary<string, object>> GetAllDefinitions();
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
