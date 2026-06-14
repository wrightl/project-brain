namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Repositories;

public class CountryService : ICountryService
{
    private const string ActiveCountriesCacheKey = "countries:active";
    private static readonly TimeSpan ActiveCountriesCacheExpiration = TimeSpan.FromHours(24);

    private readonly ICountryRepository _repository;
    private readonly ICacheService _cache;

    public CountryService(ICountryRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<List<CountryOptionDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<CountryOptionDto>>(ActiveCountriesCacheKey, cancellationToken);
        if (cached is { Count: > 0 })
        {
            return cached;
        }

        var countries = await _repository.GetAllActiveAsync(cancellationToken);

        if (countries.Count > 0)
        {
            await _cache.SetAsync(
                ActiveCountriesCacheKey,
                countries,
                ActiveCountriesCacheExpiration,
                cancellationToken);
        }

        return countries;
    }
}

public interface ICountryService
{
    Task<List<CountryOptionDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
