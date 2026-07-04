namespace ProjectBrain.Domain.Repositories;

using System;
using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;

/// <summary>
/// Repository implementation for CoachProfile entity
/// </summary>
public class CoachProfileRepository : Repository<CoachProfile, int>, ICoachProfileRepository
{
    public CoachProfileRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CoachProfile?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.Id == id, cancellationToken);
    }

    public async Task<CoachProfile?> GetByIdWithRelatedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
            .FirstOrDefaultAsync(cp => cp.Id == id, cancellationToken);
    }

    public async Task<CoachProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.UserId == userId, cancellationToken);
    }

    public async Task<CoachProfile?> GetByUserIdWithRelatedAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles)
            .FirstOrDefaultAsync(cp => cp.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<CoachProfile>> GetByUserIdsWithRelatedAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<CoachProfile>();
        }

        return await _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles)
            .Where(cp => idList.Contains(cp.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CoachProfile>> SearchAsync(
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CoachProfile> query = _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles);

        // Filter by location
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(cp => cp.User != null && cp.User.City != null && cp.User.City.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(stateProvince))
        {
            query = query.Where(cp => cp.User != null && cp.User.StateProvince != null && cp.User.StateProvince.Contains(stateProvince));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(cp => cp.User != null && cp.User.Country != null && cp.User.Country.Contains(country));
        }

        // Filter by age groups
        if (ageGroups != null && ageGroups.Any())
        {
            var ageGroupList = ageGroups.ToList();
            query = query.Where(cp => cp.AgeGroups == null || cp.AgeGroups.Count == 0 || cp.AgeGroups.Any(ag => ageGroupList.Contains(ag.AgeGroup)));
        }

        // Filter by specialisms
        if (specialisms != null && specialisms.Any())
        {
            var specialismList = specialisms.ToList();
            query = query.Where(cp => cp.Specialisms == null || cp.Specialisms.Count == 0 || cp.Specialisms.Any(s => specialismList.Contains(s.Specialism)));
        }

        // Only return coaches that are onboarded
        query = query.Where(cp => cp.User != null && cp.User.IsOnboarded);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CoachProfile>> SearchByDistanceAsync(
        double centerLatitude,
        double centerLongitude,
        double radiusMiles,
        IEnumerable<string>? ageGroups = null,
        IEnumerable<string>? specialisms = null,
        CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(centerLatitude) ||
            !double.IsFinite(centerLongitude) ||
            !double.IsFinite(radiusMiles) ||
            radiusMiles <= 0)
        {
            return Array.Empty<CoachProfile>();
        }

        IQueryable<CoachProfile> query = _dbSet
            .AsNoTracking()
            .Include(cp => cp.Qualifications)
            .Include(cp => cp.Specialisms)
            .Include(cp => cp.AgeGroups)
            .Include(cp => cp.User!)
                .ThenInclude(u => u!.UserRoles);

        // Only return coaches that are onboarded
        query = query.Where(cp => cp.User != null && cp.User.IsOnboarded);

        // Filter by age groups (same semantics as text search)
        if (ageGroups != null && ageGroups.Any())
        {
            var ageGroupList = ageGroups.ToList();
            query = query.Where(cp => cp.AgeGroups == null || cp.AgeGroups.Count == 0 || cp.AgeGroups.Any(ag => ageGroupList.Contains(ag.AgeGroup)));
        }

        // Filter by specialisms (same semantics as text search)
        if (specialisms != null && specialisms.Any())
        {
            var specialismList = specialisms.ToList();
            query = query.Where(cp => cp.Specialisms == null || cp.Specialisms.Count == 0 || cp.Specialisms.Any(s => specialismList.Contains(s.Specialism)));
        }

        // Coaches must have coordinates to participate in geo search
        query = query.Where(cp => cp.User != null && cp.User.Latitude != null && cp.User.Longitude != null);

        var bbox = ComputeBoundingBoxMiles(centerLatitude, centerLongitude, radiusMiles);

        query = query.Where(cp => cp.User!.Latitude!.Value >= bbox.MinLat && cp.User!.Latitude!.Value <= bbox.MaxLat);

        if (!bbox.CrossesAntimeridian)
        {
            query = query.Where(cp => cp.User!.Longitude!.Value >= bbox.MinLon && cp.User!.Longitude!.Value <= bbox.MaxLon);
        }
        else
        {
            // Wrap-around range (e.g. 170..180 OR -180..-170)
            query = query.Where(cp => cp.User!.Longitude!.Value >= bbox.MinLon || cp.User!.Longitude!.Value <= bbox.MaxLon);
        }

        var candidates = await query.ToListAsync(cancellationToken);

        var filtered = candidates
            .Select(cp => new
            {
                CoachProfile = cp,
                Distance = HaversineMiles(
                    centerLatitude,
                    centerLongitude,
                    cp.User!.Latitude!.Value,
                    cp.User!.Longitude!.Value)
            })
            .Where(x => x.Distance <= radiusMiles)
            .OrderBy(x => x.Distance)
            .Select(x => x.CoachProfile)
            .ToList();

        return filtered;
    }

    private readonly record struct BoundingBox(
        double MinLat,
        double MaxLat,
        double MinLon,
        double MaxLon,
        bool CrossesAntimeridian);

    private static BoundingBox ComputeBoundingBoxMiles(
        double centerLatitude,
        double centerLongitude,
        double radiusMiles)
    {
        const double MilesPerDegreeLat = 69.0;

        var latDelta = radiusMiles / MilesPerDegreeLat;
        var minLat = ClampLatitude(centerLatitude - latDelta);
        var maxLat = ClampLatitude(centerLatitude + latDelta);

        var latRad = DegreesToRadians(centerLatitude);
        var milesPerDegreeLon = MilesPerDegreeLat * Math.Cos(latRad);
        var lonDelta = milesPerDegreeLon <= 0.000001 ? 180d : radiusMiles / milesPerDegreeLon;

        var rawMinLon = centerLongitude - lonDelta;
        var rawMaxLon = centerLongitude + lonDelta;

        var crosses = rawMinLon < -180d || rawMaxLon > 180d;
        var minLon = NormalizeLongitude(rawMinLon);
        var maxLon = NormalizeLongitude(rawMaxLon);

        return new BoundingBox(minLat, maxLat, minLon, maxLon, crosses);
    }

    private static double ClampLatitude(double latitude) =>
        Math.Max(-90d, Math.Min(90d, latitude));

    private static double NormalizeLongitude(double longitude)
    {
        var lon = longitude;
        while (lon < -180d) lon += 360d;
        while (lon > 180d) lon -= 360d;
        return lon;
    }

    private static double DegreesToRadians(double degrees) =>
        degrees * (Math.PI / 180d);

    private static double HaversineMiles(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double EarthRadiusMiles = 3958.7613;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a =
            Math.Pow(Math.Sin(dLat / 2d), 2d) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Pow(Math.Sin(dLon / 2d), 2d);

        var c = 2d * Math.Asin(Math.Min(1d, Math.Sqrt(a)));
        return EarthRadiusMiles * c;
    }

    public async Task<IEnumerable<CoachProfile>> GetByIdsWithUserAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<CoachProfile>();
        }

        return await _dbSet
            .AsNoTracking()
            .Include(cp => cp.User)
            .Where(cp => idList.Contains(cp.Id))
            .ToListAsync(cancellationToken);
    }
}

