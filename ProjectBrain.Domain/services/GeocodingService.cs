namespace ProjectBrain.Domain;

using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public record LatLng(double Latitude, double Longitude);

public interface IGeocodingService
{
    Task<LatLng?> GeocodeAsync(
        string? city,
        string? stateProvince,
        string? country,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Server-side geocoding via Google Maps Platform Geocoding API.
/// </summary>
public class GoogleGeocodingService : IGeocodingService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<GoogleGeocodingService> _logger;

    public GoogleGeocodingService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        ILogger<GoogleGeocodingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<LatLng?> GeocodeAsync(
        string? city,
        string? stateProvince,
        string? country,
        CancellationToken cancellationToken = default)
    {
        var addressParts = new[] { city, stateProvince, country }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .ToArray();

        if (addressParts.Length == 0)
        {
            return null;
        }

        var address = string.Join(", ", addressParts);
        var cacheKey = BuildCacheKey(address);

        if (_memoryCache.TryGetValue(cacheKey, out LatLng? cached) && cached is not null)
        {
            return cached;
        }

        var apiKey = _configuration["GoogleMaps:GeocodingApiKey"]
                     ?? _configuration["GOOGLE_MAPS_GEOCODING_API_KEY"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Google geocoding API key is not configured. Set GoogleMaps:GeocodingApiKey or GOOGLE_MAPS_GEOCODING_API_KEY.");
        }

        var url =
            $"json?address={Uri.EscapeDataString(address)}&key={Uri.EscapeDataString(apiKey)}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Geocoding API request failed with {StatusCode} for address '{Address}'",
                    (int)response.StatusCode,
                    address);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("status", out var statusEl))
            {
                _logger.LogWarning("Google Geocoding API response missing status for address '{Address}'", address);
                return null;
            }

            var status = statusEl.GetString();
            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                // Common non-error statuses: ZERO_RESULTS, OVER_QUERY_LIMIT, REQUEST_DENIED, INVALID_REQUEST
                _logger.LogInformation("Google Geocoding API status '{Status}' for address '{Address}'", status, address);
                return null;
            }

            var results = doc.RootElement.GetProperty("results");
            if (results.GetArrayLength() == 0)
            {
                return null;
            }

            var location = results[0].GetProperty("geometry").GetProperty("location");
            var lat = location.GetProperty("lat").GetDouble();
            var lng = location.GetProperty("lng").GetDouble();

            var result = new LatLng(lat, lng);
            _memoryCache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Geocoding API for address '{Address}'", address);
            return null;
        }
    }

    private static string BuildCacheKey(string address)
    {
        return $"geocode:{address.Trim().ToLowerInvariant()}";
    }
}

