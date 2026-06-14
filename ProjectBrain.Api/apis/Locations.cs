using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class LocationServices(
    ILogger<LocationServices> logger,
    IIdentityService identityService,
    IConfiguration configuration,
    IMemoryCache memoryCache,
    IHttpClientFactory httpClientFactory,
    ICountryService countryService)
{
    public ILogger<LocationServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IConfiguration Configuration { get; } = configuration;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    public ICountryService CountryService { get; } = countryService;
}

public static class LocationEndpoints
{
    public static void MapLocationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("locations").RequireAuthorization();
        group.MapGet("/countries", GetCountries).WithName("GetCountries");
        group.MapGet("/cities", SearchCities).WithName("SearchCities");
    }

    private static async Task<IResult> GetCountries(
        [AsParameters] LocationServices services,
        CancellationToken cancellationToken)
    {
        var countries = await services.CountryService.GetAllActiveAsync(cancellationToken);
        var response = countries
            .Select(c => new CountryOptionResponse
            {
                Name = c.Name,
                Code = c.Code,
            })
            .ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> SearchCities(
        [AsParameters] LocationServices services,
        [FromQuery] string q,
        [FromQuery] string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return Results.Ok(Array.Empty<CityOptionResponse>());
        }

        q = q?.Trim() ?? string.Empty;
        countryCode = countryCode.Trim();

        if (q.Length < 2)
        {
            return Results.Ok(Array.Empty<CityOptionResponse>());
        }

        var apiKey =
            services.Configuration["GOOGLE_MAPS_GEOCODING_API_KEY"] ??
            services.Configuration["GoogleMaps:GeocodingApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Results.Problem(
                detail: "Google Places API key is not configured.",
                statusCode: 500);
        }

        var cacheKey = $"places:cities:{countryCode.ToLowerInvariant()}:{q.ToLowerInvariant()}";
        if (services.MemoryCache.TryGetValue(cacheKey, out List<CityOptionResponse>? cached) && cached != null)
        {
            return Results.Ok(cached);
        }

        var http = services.HttpClientFactory.CreateClient();

        // Places API (New)
        // Autocomplete: POST https://places.googleapis.com/v1/places:autocomplete
        // Details: GET https://places.googleapis.com/v1/places/{placeId}

        var autocompleteBody = new PlacesAutocompleteNewRequest
        {
            Input = q,
            IncludedRegionCodes = new List<string> { countryCode.ToUpperInvariant() },
            // Prefer city/town-like results, but be tolerant if Google returns other locality-ish matches.
            IncludedPrimaryTypes = new List<string> { "locality" }
        };

        using var autocompleteRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://places.googleapis.com/v1/places:autocomplete");
        autocompleteRequest.Headers.Add("X-Goog-Api-Key", apiKey);
        autocompleteRequest.Headers.Add(
            "X-Goog-FieldMask",
            "suggestions.placePrediction.placeId,suggestions.placePrediction.types,suggestions.placePrediction.text.text");
        autocompleteRequest.Content = new StringContent(
            JsonSerializer.Serialize(autocompleteBody),
            System.Text.Encoding.UTF8,
            "application/json");

        using var autocompleteResponse = await http.SendAsync(autocompleteRequest);
        if (!autocompleteResponse.IsSuccessStatusCode)
        {
            services.Logger.LogWarning("Places API (New) autocomplete failed {StatusCode}", (int)autocompleteResponse.StatusCode);
            return Results.Ok(Array.Empty<CityOptionResponse>());
        }

        var autocompleteJson = await autocompleteResponse.Content.ReadAsStringAsync();
        var autocomplete = JsonSerializer.Deserialize<PlacesAutocompleteNewResponse>(autocompleteJson);

        var placeIds = (autocomplete?.Suggestions ?? new List<PlacesAutocompleteSuggestion>())
            .Select(s => s.PlacePrediction?.PlaceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        if (!placeIds.Any())
        {
            return Results.Ok(Array.Empty<CityOptionResponse>());
        }

        var tasks = placeIds.Select(async placeId =>
        {
            using var detailsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}");
            detailsRequest.Headers.Add("X-Goog-Api-Key", apiKey);
            detailsRequest.Headers.Add(
                "X-Goog-FieldMask",
                "id,formattedAddress,location,addressComponents");

            using var detailsResponse = await http.SendAsync(detailsRequest);
            if (!detailsResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var detailsJson = await detailsResponse.Content.ReadAsStringAsync();
            var place = JsonSerializer.Deserialize<PlaceDetailsNewResponse>(detailsJson);
            if (place == null || place.Location == null)
            {
                return null;
            }

            var city =
                GetComponentNew(place.AddressComponents, "locality") ??
                GetComponentNew(place.AddressComponents, "postal_town") ??
                GetComponentNew(place.AddressComponents, "administrative_area_level_2");

            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            var stateProvince = GetComponentNew(place.AddressComponents, "administrative_area_level_1");
            var country = GetComponentNew(place.AddressComponents, "country");

            return new CityOptionResponse
            {
                City = city!,
                StateProvince = stateProvince,
                Country = country,
                Latitude = place.Location.Latitude,
                Longitude = place.Location.Longitude,
                PlaceId = place.Id ?? placeId,
                FormattedAddress = place.FormattedAddress ?? city!
            };
        });

        var results = (await Task.WhenAll(tasks))
            .Where(x => x != null)
            .Cast<CityOptionResponse>()
            .GroupBy(x => $"{x.City}|{x.StateProvince}|{x.Country}".ToLowerInvariant())
            .Select(g => g.First())
            .Take(10)
            .ToList();

        services.MemoryCache.Set(cacheKey, results, TimeSpan.FromMinutes(30));
        return Results.Ok(results);
    }

    private static string? GetComponentNew(List<AddressComponentNew>? components, string type)
    {
        return components?.FirstOrDefault(c => c.Types != null && c.Types.Contains(type))?.LongText;
    }

    private class PlacesAutocompleteNewRequest
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("includedRegionCodes")]
        public List<string>? IncludedRegionCodes { get; set; }

        [JsonPropertyName("includedPrimaryTypes")]
        public List<string>? IncludedPrimaryTypes { get; set; }
    }

    private class PlacesAutocompleteNewResponse
    {
        [JsonPropertyName("suggestions")]
        public List<PlacesAutocompleteSuggestion>? Suggestions { get; set; }
    }

    private class PlacesAutocompleteSuggestion
    {
        [JsonPropertyName("placePrediction")]
        public PlacePrediction? PlacePrediction { get; set; }
    }

    private class PlacePrediction
    {
        [JsonPropertyName("placeId")]
        public string? PlaceId { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }

        [JsonPropertyName("text")]
        public PredictionText? Text { get; set; }
    }

    private class PredictionText
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class PlaceDetailsNewResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("formattedAddress")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("location")]
        public LatLngNew? Location { get; set; }

        [JsonPropertyName("addressComponents")]
        public List<AddressComponentNew>? AddressComponents { get; set; }
    }

    private class LatLngNew
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private class AddressComponentNew
    {
        [JsonPropertyName("longText")]
        public string LongText { get; set; } = string.Empty;

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }
    }
}

public class CountryOptionResponse
{
    public required string Name { get; init; }
    public required string Code { get; init; }
}

public class CityOptionResponse
{
    public required string City { get; init; }
    public string? StateProvince { get; init; }
    public string? Country { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public required string PlaceId { get; init; }
    public required string FormattedAddress { get; init; }
}

