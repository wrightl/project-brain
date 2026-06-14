namespace ProjectBrain.Domain.Repositories;

public interface ICountryRepository
{
    Task<List<CountryOptionDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

public record CountryOptionDto(string Name, string Code);
