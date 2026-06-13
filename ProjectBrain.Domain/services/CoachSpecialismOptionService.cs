namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain.Repositories;

public class CoachSpecialismOptionService : ICoachSpecialismOptionService
{
    private const string ActiveSpecialismsCacheKey = "coachspecialismoptions:active";
    private static readonly TimeSpan ActiveSpecialismsCacheExpiration = TimeSpan.FromHours(24);

    private readonly ICoachSpecialismOptionRepository _repository;
    private readonly ICacheService _cache;

    public CoachSpecialismOptionService(
        ICoachSpecialismOptionRepository repository,
        ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<List<string>> GetActiveNamesAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<string>>(ActiveSpecialismsCacheKey, cancellationToken);
        if (cached is { Count: > 0 })
        {
            return cached;
        }

        var names = await _repository.GetActiveNamesAsync(cancellationToken);

        if (names.Count > 0)
        {
            await _cache.SetAsync(
                ActiveSpecialismsCacheKey,
                names,
                ActiveSpecialismsCacheExpiration,
                cancellationToken);
        }

        return names;
    }

    public async Task ValidateSpecialismsAsync(
        IEnumerable<string>? specialisms,
        CancellationToken cancellationToken = default)
    {
        if (specialisms is null)
        {
            return;
        }

        var requested = specialisms
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct()
            .ToList();

        if (requested.Count == 0)
        {
            return;
        }

        var activeNames = await GetActiveNamesAsync(cancellationToken);
        var activeSet = activeNames.ToHashSet(StringComparer.Ordinal);
        var invalid = requested.Where(s => !activeSet.Contains(s)).ToList();

        if (invalid.Count > 0)
        {
            throw new AppException(
                "INVALID_SPECIALISM",
                $"Unknown specialism(s): {string.Join(", ", invalid)}",
                400);
        }
    }
}

public interface ICoachSpecialismOptionService
{
    Task<List<string>> GetActiveNamesAsync(CancellationToken cancellationToken = default);
    Task ValidateSpecialismsAsync(IEnumerable<string>? specialisms, CancellationToken cancellationToken = default);
}
