namespace ProjectBrain.Domain.Repositories;

using Microsoft.EntityFrameworkCore;

public class CountryRepository : ICountryRepository
{
    private readonly AppDbContext _context;

    public CountryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CountryOptionDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CountryOptionDto(c.Name, c.Code))
            .ToListAsync(cancellationToken);
    }
}
