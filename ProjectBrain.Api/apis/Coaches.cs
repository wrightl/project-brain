using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Database;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Shared.Constants;
using ProjectBrain.Database.Models;
using ProjectBrain.Shared.Dtos.CoachRatings;
using ProjectBrain.Shared.Dtos.Pagination;

public class CoachServices(
    ILogger<CoachServices> logger,
    IIdentityService identityService,
    ICoachProfileService coachProfileService,
    IUserService userService,
    IConnectionService connectionService,
    IUserActivityService userActivityService,
    IUserProfileService userProfileService,
    IFeatureGateService featureGateService,
    ISubscriptionService subscriptionService,
    IUsageTrackingService usageTrackingService,
    ICoachMessageService coachMessageService,
    ICoachRatingService coachRatingService,
    IGeocodingService geocodingService,
    IFakeCoachAutoAcceptService fakeCoachAutoAcceptService,
    ICoachSpecialismOptionService coachSpecialismOptionService,
    IConfiguration configuration)
{
    public ILogger<CoachServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public ICoachProfileService CoachProfileService { get; } = coachProfileService;
    public IUserService UserService { get; } = userService;
    public IConnectionService ConnectionService { get; } = connectionService;
    public IUserActivityService UserActivityService { get; } = userActivityService;
    public IUserProfileService UserProfileService { get; } = userProfileService;
    public IFeatureGateService FeatureGateService { get; } = featureGateService;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
    public IUsageTrackingService UsageTrackingService { get; } = usageTrackingService;
    public ICoachMessageService CoachMessageService { get; } = coachMessageService;
    public ICoachRatingService CoachRatingService { get; } = coachRatingService;
    public IGeocodingService GeocodingService { get; } = geocodingService;
    public IFakeCoachAutoAcceptService FakeCoachAutoAcceptService { get; } = fakeCoachAutoAcceptService;
    public ICoachSpecialismOptionService CoachSpecialismOptionService { get; } = coachSpecialismOptionService;
    public IConfiguration Configuration { get; } = configuration;
}

public static class CoachEndpoints
{
    public static void MapCoachEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("coaches").RequireAuthorization();

        group.MapGet("/search", SearchCoaches).WithName("SearchCoaches");
        group.MapGet("/specialisms", GetCoachSpecialisms).WithName("GetCoachSpecialisms");
        group.MapGet("/connected", GetConnectedCoaches).WithName("GetConnectedCoaches");
        group.MapGet("/clients", GetConnectedClients).WithName("GetConnectedClients").RequireAuthorization("CoachOnly");
        group.MapPost("/clients/{userId}/accept", AcceptClientConnection).WithName("AcceptClientConnection").RequireAuthorization("CoachOnly");

        group.MapPost("/connection-statuses", GetBatchConnectionStatuses).WithName("GetBatchConnectionStatuses");
        group.MapPost("/summaries", GetBatchCoachSummaries).WithName("GetBatchCoachSummaries");
        group.MapGet("/{id}/connection-status", GetConnectionStatus).WithName("GetConnectionStatus");
        group.MapPost("/{coachId}/connections", SendConnectionRequest).WithName("SendConnectionRequest");
        group.MapDelete("/{id}/connections", CancelConnectionRequest).WithName("CancelConnectionRequest");

        group.MapGet("/{id}", GetCoachById).WithName("GetCoachById");
        group.MapGet("/{userId}/profile", GetCoachProfileByUserId).WithName("GetCoachProfileByUserId");
        group.MapPut("/me/{userId}", UpdateCoach).WithName("UpdateCoach").RequireAuthorization("CoachOnly");

        group.MapGet("/availability/status", GetAvailabilityStatus).WithName("GetAvailabilityStatus").RequireAuthorization("CoachOnly");
        group.MapPut("/availability/status", SetAvailabilityStatus).WithName("SetAvailabilityStatus").RequireAuthorization("CoachOnly");

        // Rating endpoints
        group.MapPost("/{id}/ratings", CreateOrUpdateRating).WithName("CreateOrUpdateRating");
        group.MapGet("/{id}/ratings", GetRatings).WithName("GetRatings");
        group.MapGet("/ratings/mine", GetMyRatings).WithName("GetMyRatings").RequireAuthorization("CoachOnly");
        group.MapGet("/{id}/ratings/me", GetMyRating).WithName("GetMyRating");
    }

    private static async Task<IResult> GetConnectedCoaches(
        [AsParameters] CoachServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            // Get all connected coach IDs for the current user (with status)
            var connectedCoaches = await services.ConnectionService.GetConnectedCoachIdsAsync(userId);

            if (!connectedCoaches.Any())
            {
                return Results.Ok(new List<CoachWithConnectionStatusDto>());
            }

            var coachIds = connectedCoaches.Select(c => c.CoachId).Distinct().ToList();
            var connectionByCoachId = connectedCoaches
                .GroupBy(c => c.CoachId)
                .ToDictionary(g => g.Key, g => g.First());

            var coachProfiles = await services.CoachProfileService.GetByUserIdsWithRelatedAsync(coachIds);

            var coachDtos = coachProfiles
                .Where(cp => cp.User != null)
                .Select(cp => cp.ToCoachDto())
                .ToList();

            await coachDtos.SetOnlineStatusAsync(
                services.UserActivityService,
                services.CoachMessageService,
                activityWindowMinutes: 30,
                configuration: services.Configuration);

            var coachesWithStatus = coachDtos.Select(coachDto =>
            {
                connectionByCoachId.TryGetValue(coachDto.Id, out var connection);

                return new CoachWithConnectionStatusDto
                {
                    Id = coachDto.Id,
                    Email = coachDto.Email,
                    FullName = coachDto.FullName,
                    Roles = coachDto.Roles,
                    IsOnboarded = coachDto.IsOnboarded,
                    LastActivityAt = coachDto.LastActivityAt,
                    StreetAddress = coachDto.StreetAddress,
                    AddressLine2 = coachDto.AddressLine2,
                    City = coachDto.City,
                    StateProvince = coachDto.StateProvince,
                    PostalCode = coachDto.PostalCode,
                    Country = coachDto.Country,
                    Qualifications = coachDto.Qualifications,
                    Specialisms = coachDto.Specialisms,
                    AgeGroups = coachDto.AgeGroups,
                    AvailabilityStatus = coachDto.AvailabilityStatus,
                    IsOnline = coachDto.AvailabilityStatus == AvailabilityStatus.Available,
                    ConnectionStatus = connection?.Status ?? "pending",
                    RequestedAt = connection?.RequestedAt ?? DateTime.UtcNow,
                    RequestedBy = connection?.RequestedBy ?? AppRoles.User,
                    Message = connection?.Message,
                };
            }).ToList();

            // Sort: online coaches first, then alphabetically by full name
            var sortedCoaches = coachesWithStatus
                .OrderByDescending(c => c.IsOnline)
                .ThenBy(c => c.FullName)
                .ToList();

            return Results.Ok(sortedCoaches);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving connected coaches for user {UserId}", userId);
            return Results.Problem(
                detail: "An error occurred while retrieving connected coaches",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetConnectedClients(
        [AsParameters] CoachServices services)
    {
        var coachId = services.IdentityService.UserId!;

        try
        {
            // Get all connections for the current coach (both accepted and pending)
            var connections = await services.ConnectionService.GetConnectionsByCoachIdAsync(coachId);

            if (!connections.Any())
            {
                return Results.Ok(new List<ClientWithConnectionStatusDto>());
            }

            var userIds = connections.Select(c => c.UserId).Distinct().ToList();
            var users = await services.UserService.GetByIds(userIds);
            var usersById = users.ToDictionary(u => u.Id);
            var profilesByUserId = await services.UserProfileService.GetByUserIds(userIds);
            var coachCountsByUserId = await services.ConnectionService.GetConnectedCoachCountsByUserIdsAsync(userIds);
            var earliestDatesByUserId = await services.ConnectionService.GetEarliestConnectionDatesByUserIdsAsync(userIds);

            var clientDtos = new List<ClientWithConnectionStatusDto>();
            foreach (var connectionWithStatus in connections)
            {
                if (!usersById.TryGetValue(connectionWithStatus.UserId, out var baseUser))
                {
                    continue;
                }

                profilesByUserId.TryGetValue(connectionWithStatus.UserId, out var userProfile);

                var user = new UserDto
                {
                    Id = baseUser.Id,
                    Email = baseUser.Email,
                    FullName = baseUser.FullName,
                    Roles = baseUser.Roles,
                    IsOnboarded = baseUser.IsOnboarded,
                    LastActivityAt = baseUser.LastActivityAt,
                    StreetAddress = baseUser.StreetAddress,
                    AddressLine2 = baseUser.AddressLine2,
                    City = baseUser.City,
                    StateProvince = baseUser.StateProvince,
                    PostalCode = baseUser.PostalCode,
                    Country = baseUser.Country,
                    Latitude = baseUser.Latitude,
                    Longitude = baseUser.Longitude,
                    Connection = baseUser.Connection,
                    EmailVerified = baseUser.EmailVerified,
                    DoB = userProfile?.DoB,
                    PreferredPronoun = userProfile?.PreferredPronoun,
                    NeurodiverseTraits = userProfile?.NeurodiverseTraits?.Select(t => t.Trait).ToList() ?? new List<string>(),
                };

                coachCountsByUserId.TryGetValue(connectionWithStatus.UserId, out var coachesCount);
                earliestDatesByUserId.TryGetValue(connectionWithStatus.UserId, out var earliestConnectionDate);

                TimeSpan? timeOnPlatform = earliestConnectionDate != default
                    ? DateTime.UtcNow - earliestConnectionDate
                    : null;

                int? age = null;
                if (user.DoB.HasValue)
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    age = today.Year - user.DoB.Value.Year;
                    if (user.DoB.Value > today.AddYears(-age.Value))
                    {
                        age--;
                    }
                }

                clientDtos.Add(new ClientWithConnectionStatusDto
                {
                    User = user,
                    ConnectionStatus = connectionWithStatus.Status,
                    RequestedAt = connectionWithStatus.RequestedAt,
                    RequestedBy = connectionWithStatus.RequestedBy ?? AppRoles.User,
                    Message = connectionWithStatus.Message,
                    NeurodiverseTraits = user.NeurodiverseTraits ?? new List<string>(),
                    PreferredPronoun = user.PreferredPronoun,
                    Age = age,
                    ConnectedCoachesCount = coachesCount,
                    TimeOnPlatform = timeOnPlatform,
                });
            }

            // Sort: accepted first, then pending; within each group, online users first, then alphabetically
            var sortedClients = clientDtos
                .OrderByDescending(c => c.ConnectionStatus == "accepted")
                .ThenByDescending(c => c.User.LastActivityAt.HasValue &&
                    (DateTime.UtcNow - c.User.LastActivityAt.Value).TotalMinutes <= 30)
                .ThenBy(c => c.User.FullName)
                .ToList();

            return Results.Ok(sortedClients);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving connected clients for coach {CoachId}", coachId);
            return Results.Problem(
                detail: "An error occurred while retrieving connected clients",
                statusCode: 500);
        }
    }

    private static async Task<IResult> AcceptClientConnection(
        [AsParameters] CoachServices services,
        string userId)
    {
        var coachId = services.IdentityService.UserId!;

        try
        {
            // Accept the connection request
            var success = await services.ConnectionService.AcceptConnectionAsync(userId, coachId);

            if (!success)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "CONNECTION_NOT_FOUND_OR_INVALID",
                        Message = "Connection request not found or is not in pending status"
                    }
                });
            }

            // Return the updated connection status
            var connection = await services.ConnectionService.GetConnectionAsync(userId, coachId);
            if (connection == null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "CONNECTION_NOT_FOUND",
                        Message = "Connection not found after acceptance"
                    }
                });
            }

            var response = new ConnectionStatusResponse
            {
                Status = "connected",
                RequestedAt = connection.RequestedAt,
                RespondedAt = connection.RespondedAt,
                RequestedBy = connection.RequestedBy
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error accepting connection for coach {CoachId} and user {UserId}", coachId, userId);
            return Results.Problem(
                detail: "An error occurred while accepting the connection",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetCoachSpecialisms(
        [AsParameters] CoachServices services)
    {
        var specialisms = await services.CoachSpecialismOptionService.GetActiveNamesAsync();
        return Results.Ok(specialisms);
    }

    private static async Task<IResult> SearchCoaches(
        [AsParameters] CoachServices services,
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        string? useMyLocation = null,
        string? distanceMiles = null,
        string? latitude = null,
        string? longitude = null,
        [FromQuery] string[]? ageGroups = null,
        [FromQuery] string[]? specialisms = null)
    {
        static bool? ParseBoolish(string? value)
        {
            if (value is null) return null;
            var v = value.Trim().ToLowerInvariant();
            return v switch
            {
                "true" or "1" or "yes" or "on" => true,
                "false" or "0" or "no" or "off" => false,
                _ => null
            };
        }

        static bool TryParseFiniteDouble(string? value, out double result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }
            if (!double.IsFinite(parsed)) return false;
            result = parsed;
            return true;
        }

        var useMyLocationParsed = ParseBoolish(useMyLocation) == true;

        double? radiusMiles = null;
        if (!string.IsNullOrWhiteSpace(distanceMiles))
        {
            if (!TryParseFiniteDouble(distanceMiles, out var parsedRadius) || parsedRadius <= 0)
            {
                return Results.BadRequest("distanceMiles must be a positive number.");
            }
            radiusMiles = parsedRadius;
        }

        List<CoachProfile> coaches;

        if (useMyLocationParsed)
        {
            if (radiusMiles is null)
            {
                return Results.BadRequest("distanceMiles is required when useMyLocation=true.");
            }

            var currentUserId = services.IdentityService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Results.Unauthorized();
            }

            var currentUser = await services.UserService.GetById(currentUserId);
            if (currentUser is null)
            {
                return Results.NotFound($"User with ID {currentUserId} not found.");
            }

            if (currentUser.Latitude is null || currentUser.Longitude is null)
            {
                return Results.BadRequest("Your profile does not have location coordinates yet.");
            }

            coaches = await services.CoachProfileService.SearchByDistance(
                centerLatitude: currentUser.Latitude.Value,
                centerLongitude: currentUser.Longitude.Value,
                radiusMiles: radiusMiles.Value,
                ageGroups: ageGroups,
                specialisms: specialisms);
        }
        else if (radiusMiles is not null)
        {
            // Geo search requested with explicit center point
            if (!TryParseFiniteDouble(latitude, out var centerLat) ||
                !TryParseFiniteDouble(longitude, out var centerLon))
            {
                return Results.BadRequest("latitude and longitude are required when distanceMiles is provided.");
            }

            coaches = await services.CoachProfileService.SearchByDistance(
                centerLatitude: centerLat,
                centerLongitude: centerLon,
                radiusMiles: radiusMiles.Value,
                ageGroups: ageGroups,
                specialisms: specialisms);
        }
        else
        {
            // Fallback: string-based search
            coaches = await services.CoachProfileService.Search(
                city: city,
                stateProvince: stateProvince,
                country: country,
                ageGroups: ageGroups,
                specialisms: specialisms);
        }

        var coachDtos = coaches
            .Where(cp => cp.User != null)
            .Select(cp => cp.ToCoachDto())
            .ToList();

        // Set online status for all coaches (30-minute window for coaches)
        await coachDtos.SetOnlineStatusAsync(
            services.UserActivityService,
            services.CoachMessageService,
            activityWindowMinutes: 30,
            configuration: services.Configuration);

        // Populate rating data for all coaches
        foreach (var coachDto in coachDtos)
        {
            coachDto.AverageRating = await services.CoachRatingService.GetAverageRatingAsync(coachDto.Id);
            coachDto.RatingCount = await services.CoachRatingService.GetRatingCountAsync(coachDto.Id);
        }

        return Results.Ok(coachDtos);
    }

    private static async Task<IResult> GetCoachById(
        [AsParameters] CoachServices services,
        string id)
    {
        var coachId = int.Parse(id);
        var coachProfile = await services.CoachProfileService.GetByIdWithRelated(coachId);

        if (coachProfile == null || coachProfile.User == null)
        {
            return Results.NotFound();
        }

        // Check whether the user is connected to the coach
        var userId = services.IdentityService.UserId!;
        // var connection = await services.ConnectionService.GetConnectionAsync(userId, coachProfile.UserId);

        var coachDto = coachProfile.ToCoachDto();
        // var coachProfileWithConnection = new CoachWithConnectionStatusDto
        // {
        //     // Copy all properties from coachDto
        //     Id = coachDto.Id,
        //     Email = coachDto.Email,
        //     FullName = coachDto.FullName,
        //     Roles = coachDto.Roles,
        //     IsOnboarded = coachDto.IsOnboarded,
        //     LastActivityAt = coachDto.LastActivityAt,
        //     StreetAddress = coachDto.StreetAddress,
        //     AddressLine2 = coachDto.AddressLine2,
        //     City = coachDto.City,
        //     StateProvince = coachDto.StateProvince,
        //     PostalCode = coachDto.PostalCode,
        //     Country = coachDto.Country,
        //     Qualifications = coachDto.Qualifications,
        //     Specialisms = coachDto.Specialisms,
        //     AgeGroups = coachDto.AgeGroups,
        //     AvailabilityStatus = coachDto.AvailabilityStatus,

        //     // Add the new property
        //     ConnectionStatus = connection?.Status ?? "none",
        //     RequestedAt = connection?.RequestedAt ?? DateTime.UtcNow,
        //     RequestedBy = connection?.RequestedBy ?? string.Empty,
        //     Message = connection?.Message ?? string.Empty
        // };

        // Set online status (30-minute window for coaches)
        await coachDto.SetOnlineStatusAsync(
            services.UserActivityService,
            services.CoachMessageService,
            activityWindowMinutes: 30,
            configuration: services.Configuration);

        // Populate rating data
        coachDto.AverageRating = await services.CoachRatingService.GetAverageRatingAsync(coachDto.Id);
        coachDto.RatingCount = await services.CoachRatingService.GetRatingCountAsync(coachDto.Id);

        return Results.Ok(coachDto);
    }

    private static async Task<IResult> GetCoachProfileByUserId(
        [AsParameters] CoachServices services,
        string userId)
    {
        var coachProfile = await services.CoachProfileService.GetByUserId(userId);
        if (coachProfile == null)
        {
            return Results.NotFound();
        }

        var coachDto = coachProfile.ToCoachDto();
        return Results.Ok(coachDto);
    }

    private static async Task<IResult> UpdateCoach(
        [AsParameters] CoachServices services,
        string userId,
        UpdateCoachRequest request)
    {
        var loggedInUserId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(loggedInUserId))
        {
            return Results.Unauthorized();
        }

        // Validate that the userId in the URL matches the logged-in user
        if (!string.Equals(userId, loggedInUserId, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("You can only update your own coach data.");
        }

        var existingUser = await services.UserService.GetById(userId);
        if (existingUser is null)
        {
            return Results.NotFound($"User with ID {userId} not found.");
        }

        // Verify this is a coach
        var coachProfile = await services.CoachProfileService.GetByUserId(userId);
        if (coachProfile is null)
        {
            return Results.BadRequest("User is not a coach.");
        }

        // Update user data
        var user = new UserDto()
        {
            Id = userId,
            Email = existingUser.Email, // Email should not be changed via this endpoint
            FullName = request.FullName ?? existingUser.FullName,
            IsOnboarded = existingUser.IsOnboarded, // Don't allow changing onboarding status via this endpoint
            StreetAddress = request.StreetAddress ?? existingUser.StreetAddress,
            AddressLine2 = request.AddressLine2 ?? existingUser.AddressLine2,
            City = request.City ?? existingUser.City,
            StateProvince = request.StateProvince ?? existingUser.StateProvince,
            PostalCode = request.PostalCode ?? existingUser.PostalCode,
            Country = request.Country ?? existingUser.Country,
            Roles = existingUser.Roles // Preserve existing roles
        };

        var locationChanged =
            !string.Equals(user.City, existingUser.City, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(user.StateProvince, existingUser.StateProvince, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(user.Country, existingUser.Country, StringComparison.OrdinalIgnoreCase);

        if (locationChanged)
        {
            if (!string.IsNullOrWhiteSpace(user.City) && !string.IsNullOrWhiteSpace(user.Country))
            {
                var geocoded = await services.GeocodingService.GeocodeAsync(user.City, user.StateProvince, user.Country);
                user.Latitude = geocoded?.Latitude;
                user.Longitude = geocoded?.Longitude;
            }
            else
            {
                user.Latitude = null;
                user.Longitude = null;
            }
        }
        else
        {
            user.Latitude = existingUser.Latitude;
            user.Longitude = existingUser.Longitude;
        }

        // Update user in database
        await services.UserService.Update(user);

        // Update coach profile if coach-specific fields are provided
        if (request.Qualifications != null ||
            request.Specialisms != null ||
            request.AgeGroups != null ||
            request.Bio != null ||
            request.ImageUrl != null)
        {
            await services.CoachProfileService.CreateOrUpdate(
                userId,
                qualifications: request.Qualifications,
                specialisms: request.Specialisms,
                ageGroups: request.AgeGroups,
                bio: request.Bio,
                imageUrl: request.ImageUrl);
        }

        // Return the updated coach
        var updatedCoachProfile = await services.CoachProfileService.GetByUserId(userId);
        if (updatedCoachProfile == null || updatedCoachProfile.User == null)
        {
            return Results.NotFound();
        }

        var coachDto = updatedCoachProfile.ToCoachDto();

        // Set online status (30-minute window for coaches)
        await coachDto.SetOnlineStatusAsync(
            services.UserActivityService,
            services.CoachMessageService,
            activityWindowMinutes: 30,
            configuration: services.Configuration);

        return Results.Ok(coachDto);
    }

    private static async Task<IResult> GetBatchConnectionStatuses(
        [AsParameters] CoachServices services,
        BatchConnectionStatusRequest request)
    {
        var userId = services.IdentityService.UserId!;
        var statuses = new Dictionary<string, ConnectionStatusResponse>();

        foreach (var coachProfileId in request.CoachProfileIds.Distinct())
        {
            statuses[coachProfileId] = await BuildConnectionStatusAsync(services, userId, coachProfileId);
        }

        return Results.Ok(statuses);
    }

    private static async Task<IResult> GetBatchCoachSummaries(
        [AsParameters] CoachServices services,
        BatchCoachSummariesRequest request)
    {
        var ids = request.CoachProfileIds
            .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return Results.Ok(new Dictionary<string, CoachSummaryResponse>());
        }

        var profiles = await services.CoachProfileService.GetByIdsWithUserAsync(ids);
        var summaries = profiles.ToDictionary(
            profile => profile.Id.ToString(),
            profile => new CoachSummaryResponse
            {
                CoachProfileId = profile.Id.ToString(),
                FullName = profile.User?.FullName ?? "Coach",
                Bio = profile.Bio,
                ImageUrl = profile.ImageUrl
            });

        return Results.Ok(summaries);
    }

    private static async Task<IResult> GetConnectionStatus(
        [AsParameters] CoachServices services,
        string id)
    {
        var userId = services.IdentityService.UserId!;
        var response = await BuildConnectionStatusAsync(services, userId, id);
        if (response.Status == "not_found")
        {
            return Results.NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "COACH_NOT_FOUND",
                    Message = "The specified coach does not exist"
                }
            });
        }

        return Results.Ok(response);
    }

    private static async Task<ConnectionStatusResponse> BuildConnectionStatusAsync(
        CoachServices services,
        string userId,
        string coachProfileId)
    {
        if (!int.TryParse(coachProfileId, out var profileId))
        {
            return new ConnectionStatusResponse { Status = "none" };
        }

        var coachProfile = await services.CoachProfileService.GetByIdWithRelated(profileId);
        if (coachProfile == null || coachProfile.User == null)
        {
            return new ConnectionStatusResponse { Status = "not_found" };
        }

        var coachId = coachProfile.UserId;
        var connection = await services.ConnectionService.GetConnectionAsync(userId, coachId);

        if (connection == null || connection.Status == "cancelled" || connection.Status == "rejected")
        {
            return new ConnectionStatusResponse { Status = "none" };
        }

        string apiStatus = connection.Status switch
        {
            "pending" => "pending",
            "accepted" => "connected",
            _ => "none"
        };

        return new ConnectionStatusResponse
        {
            Status = apiStatus,
            ConnectionId = connection.Id.ToString(),
            RequestedAt = connection.RequestedAt,
            RespondedAt = connection.RespondedAt,
            RequestedBy = connection.RequestedBy
        };
    }

    private static async Task<Connection> ApplyFakeCoachAutoAcceptAsync(
        CoachServices services,
        Connection connection,
        string? coachEmail)
    {
        var accepted = await services.FakeCoachAutoAcceptService.TryAutoAcceptAsync(connection, coachEmail);
        return accepted ?? connection;
    }

    private static ConnectionResponse ToConnectionResponse(Connection connection) =>
        new()
        {
            Id = connection.Id.ToString(),
            Status = connection.Status == "accepted" ? "connected" : "pending",
            RequestedAt = connection.RequestedAt,
            CoachId = connection.CoachId,
            UserId = connection.UserId
        };

    private static async Task<IResult> SendConnectionRequest(
        [AsParameters] CoachServices services,
        string coachId,
        ConnectionRequestRequest? request)
    {
        var userId = services.IdentityService.UserId!;
        var user = await services.IdentityService.GetUserAsync();
        var isCoach = user?.Roles?.Any(r => string.Equals(r, AppRoles.Coach, StringComparison.OrdinalIgnoreCase)) ?? false;
        var userType = isCoach ? UserType.Coach : UserType.User;

        // Check connection limits based on user type
        if (userType == UserType.User)
        {
            // Check coach connection limit for users
            var (allowed, errorMessage) = await services.FeatureGateService.CheckFeatureAccessAsync(userId, userType, "coach_connections");
            if (!allowed)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "CONNECTION_LIMIT_REACHED",
                        Message = errorMessage ?? "You have reached your connection limit"
                    }
                });
            }
        }
        else if (userType == UserType.Coach)
        {
            // Check client connection limit for coaches
            var (allowed, errorMessage) = await services.FeatureGateService.CheckFeatureAccessAsync(userId, userType, "client_connections");
            if (!allowed)
            {
                return Results.BadRequest(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "CONNECTION_LIMIT_REACHED",
                        Message = errorMessage ?? "You have reached your connection limit"
                    }
                });
            }
        }

        // Resolve coach by profile ID (web) or Auth0 user ID (mobile)
        CoachProfile? coachProfile = null;
        if (int.TryParse(coachId, out var profileId))
        {
            coachProfile = await services.CoachProfileService.GetByIdWithRelated(profileId);
        }

        if (coachProfile == null)
        {
            coachProfile = await services.CoachProfileService.GetByUserId(coachId);
        }

        if (coachProfile == null || coachProfile.User == null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "COACH_NOT_FOUND",
                    Message = "The specified coach does not exist"
                }
            });
        }

        var coachUserId = coachProfile.UserId;

        if (string.Equals(userId, coachUserId, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "CANNOT_CONNECT_TO_SELF",
                    Message = "You cannot send a connection request to yourself"
                }
            });
        }

        // Check if connection already exists
        var existingConnection = await services.ConnectionService.GetConnectionAsync(userId, coachUserId);

        if (existingConnection != null)
        {
            // If connection exists and is pending or accepted, return it (idempotent)
            if (existingConnection.Status == "pending" || existingConnection.Status == "accepted")
            {
                var connection = await ApplyFakeCoachAutoAcceptAsync(
                    services,
                    existingConnection,
                    coachProfile.User.Email);
                return Results.Ok(ToConnectionResponse(connection));
            }
        }

        // Create connection request
        try
        {
            var connection = await services.ConnectionService.CreateConnectionRequestAsync(
                userId,
                coachUserId,
                UserType.User.ToString(),
                request?.Message);

            connection = await ApplyFakeCoachAutoAcceptAsync(
                services,
                connection,
                coachProfile.User.Email);

            return Results.Created($"/api/coaches/{coachId}/connections", ToConnectionResponse(connection));
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating connection request");
            return Results.Problem(
                detail: "An error occurred while creating the connection request",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CancelConnectionRequest(
        [AsParameters] CoachServices services,
        string id)
    {
        var userId = services.IdentityService.UserId!;

        Connection? connection = null;

        if (Guid.TryParse(id, out var connectionId))
        {
            connection = await services.ConnectionService.GetByIdAsync(connectionId);
        }
        else
        {
            CoachProfile? coachProfile = null;
            if (int.TryParse(id, out var profileId))
            {
                coachProfile = await services.CoachProfileService.GetByIdWithRelated(profileId);
            }

            if (coachProfile == null)
            {
                coachProfile = await services.CoachProfileService.GetByUserId(id);
            }

            if (coachProfile != null)
            {
                connection = await services.ConnectionService.GetConnectionAsync(userId, coachProfile.UserId);
            }
        }

        if (connection == null)
        {
            return Results.Ok(new { message = "Connection request cancelled or removed" });
        }

        if (userId != connection.UserId && userId != connection.CoachId)
        {
            return Results.Forbid();
        }

        if (connection.Status == "pending" && !connection.RequestedBy.Equals(UserType.User.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.Forbid();
        }

        var success = await services.ConnectionService.CancelOrDeleteConnectionAsync(connection.Id);

        if (!success)
        {
            return Results.Problem(
                detail: "An error occurred while cancelling the connection request",
                statusCode: 500);
        }

        return Results.Ok(new { message = "Connection request cancelled or removed" });
    }

    private static async Task<IResult> GetAvailabilityStatus(
        [AsParameters] CoachServices services)
    {
        var userId = services.IdentityService.UserId!;

        try
        {
            var coachProfile = await services.CoachProfileService.GetByUserId(userId);
            if (coachProfile == null)
            {
                services.Logger.LogError("Coach profile not found: {UserId}", userId);
                return Results.NotFound(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "COACH_PROFILE_NOT_FOUND",
                        Message = "Coach profile not found: " + userId
                    }
                });
            }

            return Results.Ok(new { status = coachProfile.AvailabilityStatus?.ToString() ?? AvailabilityStatus.Available.ToString() });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting availability status for user {UserId}", userId);
            return Results.Problem(
                detail: "An error occurred while getting availability status",
                statusCode: 500);
        }
    }

    private static async Task<IResult> SetAvailabilityStatus(
        [AsParameters] CoachServices services,
        SetAvailabilityStatusRequest request)
    {
        var userId = services.IdentityService.UserId!;

        // Validate and parse status
        if (string.IsNullOrEmpty(request.Status) || !Enum.TryParse<AvailabilityStatus>(request.Status, ignoreCase: true, out var status))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INVALID_STATUS",
                    Message = $"Status must be one of: {string.Join(", ", Enum.GetNames(typeof(AvailabilityStatus)))}"
                }
            });
        }

        try
        {
            var coachProfile = await services.CoachProfileService.GetByUserId(userId);
            if (coachProfile == null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Error = new ErrorDetail
                    {
                        Code = "COACH_PROFILE_NOT_FOUND",
                        Message = "Coach profile not found"
                    }
                });
            }

            await services.CoachProfileService.UpdateAvailabilityStatus(userId, status);

            return Results.Ok(new { status = status.ToString() });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error setting availability status for user {UserId}", userId);
            return Results.Problem(
                detail: "An error occurred while setting availability status",
                statusCode: 500);
        }
    }

    private static async Task<IResult> CreateOrUpdateRating(
        [AsParameters] CoachServices services,
        string id,
        CreateCoachRatingRequestDto request)
    {
        var userId = services.IdentityService.UserId!;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        var coachProfileId = int.Parse(id);

        try
        {
            // id is coachProfileId, need to get the UserId
            var coachProfile = await services.CoachProfileService.GetByIdWithRelated(coachProfileId);
            if (coachProfile == null || coachProfile.User == null)
            {
                return Results.NotFound();
            }

            var rating = await services.CoachRatingService.CreateOrUpdateRatingAsync(
                userId,
                coachProfile.UserId,
                request.Rating,
                request.Feedback);

            var ratingDto = rating.ToDto();
            return Results.Ok(ratingDto);
        }
        catch (AppException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: ex.StatusCode);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating/updating rating for coach {CoachProfileId} by user {UserId}", coachProfileId, userId);
            return Results.Problem(
                detail: "An error occurred while creating/updating the rating",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetRatings(
        [AsParameters] CoachServices services,
        string id,
        int page = 1,
        int pageSize = 10)
    {
        var coachProfileId = int.Parse(id);
        try
        {
            // id is coachProfileId, need to get the UserId
            var coachProfile = await services.CoachProfileService.GetByIdWithRelated(coachProfileId);
            if (coachProfile == null || coachProfile.User == null)
            {
                return Results.NotFound();
            }

            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var skip = pagedRequest.GetSkip();
            var take = pagedRequest.GetTake();

            var ratings = await services.CoachRatingService.GetPagedRatingsByCoachIdAsync(coachProfile.UserId, skip, take);
            var totalCount = await services.CoachRatingService.GetRatingCountAsync(coachProfile.UserId);

            var ratingDtos = ratings.ToDtoList();
            var response = PagedResponse<CoachRatingResponseDto>.Create(pagedRequest, ratingDtos, totalCount);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting ratings for coach {CoachProfileId}", coachProfileId);
            return Results.Problem(
                detail: "An error occurred while getting ratings",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetMyRating(
        [AsParameters] CoachServices services,
        string id)
    {
        var userId = services.IdentityService.UserId!;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        var coachProfileId = int.Parse(id);

        try
        {
            // id is coachProfileId, need to get the UserId
            var coachProfile = await services.CoachProfileService.GetByIdWithRelated(coachProfileId);
            if (coachProfile == null || coachProfile.User == null)
            {
                return Results.NotFound();
            }

            var rating = await services.CoachRatingService.GetRatingAsync(userId, coachProfile.UserId);
            if (rating == null)
            {
                return Results.NotFound();
            }

            var ratingDto = rating.ToDto();
            return Results.Ok(ratingDto);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error getting rating for coach {CoachProfileId} by user {UserId}", coachProfileId, userId);
            return Results.Problem(
                detail: "An error occurred while getting the rating",
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetMyRatings(
        [AsParameters] CoachServices services)
    {
        var userId = services.IdentityService.UserId!;
        var coachProfile = await services.CoachProfileService.GetByUserId(userId);
        if (coachProfile == null)
        {
            return Results.NotFound();
        }
        else if (coachProfile.User?.Id != userId)
        {
            return Results.Forbid();
        }

        var ratings = await services.CoachRatingService.GetPagedRatingsByCoachIdAsync(coachProfile.UserId, 0, 10);
        var ratingDtos = ratings.ToDtoList();

        return Results.Ok(ratingDtos);
    }
}

public class UpdateCoachRequest
{
    public string? FullName { get; init; }

    // Address fields
    public string? StreetAddress { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    // Coach profile fields
    public IEnumerable<string>? Qualifications { get; init; }
    public IEnumerable<string>? Specialisms { get; init; }
    public IEnumerable<string>? AgeGroups { get; init; }
    public string? Bio { get; init; }
    public string? ImageUrl { get; init; }
}

public class ConnectionRequestRequest
{
    public string? Message { get; init; }
}

public class SetAvailabilityStatusRequest
{
    public required string Status { get; init; } // "Available", "Busy", "Away", "Offline"
}

public class BatchConnectionStatusRequest
{
    public required List<string> CoachProfileIds { get; init; }
}

public class BatchCoachSummariesRequest
{
    public required List<string> CoachProfileIds { get; init; }
}

public class CoachSummaryResponse
{
    public required string CoachProfileId { get; init; }
    public required string FullName { get; init; }
    public string? Bio { get; init; }
    public string? ImageUrl { get; init; }
}

public class ConnectionStatusResponse
{
    public required string Status { get; init; } // "none" | "pending" | "connected"
    public string? ConnectionId { get; init; }
    public DateTime? RequestedAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string? RequestedBy { get; init; } // "user" | "coach"
}

public class ConnectionResponse
{
    public required string Id { get; init; }
    public required string Status { get; init; } // "pending" | "connected"
    public required DateTime RequestedAt { get; init; }
    public required string CoachId { get; init; }
    public required string UserId { get; init; }
}

public class ErrorResponse
{
    public required ErrorDetail Error { get; init; }
}

public class ErrorDetail
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public object? Details { get; init; }
}

public class ClientWithConnectionStatusDto
{
    public required UserDto User { get; init; }
    public required string ConnectionStatus { get; init; } // "pending" or "accepted"
    public required DateTime RequestedAt { get; init; }
    public required string RequestedBy { get; init; } // "user" or "coach"
    public string? Message { get; init; }

    // Additional client information
    public List<string> NeurodiverseTraits { get; init; } = new List<string>();
    public string? PreferredPronoun { get; init; }
    public int? Age { get; init; }
    public int ConnectedCoachesCount { get; init; }
    public TimeSpan? TimeOnPlatform { get; init; } // Time since first connection (approximate)
}

public class CoachWithConnectionStatusDto
{
    // BaseUserDto properties
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public string? FirstName => FullName.Split(' ').FirstOrDefault();
    public List<string> Roles { get; init; } = new List<string>();
    public bool IsOnboarded { get; init; }
    public DateTime? LastActivityAt { get; init; }

    // Address fields
    public string? StreetAddress { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }

    // CoachDto specific properties
    public List<string> Qualifications { get; init; } = new List<string>();
    public List<string> Specialisms { get; init; } = new List<string>();
    public List<string> AgeGroups { get; init; } = new List<string>();
    public bool IsOnline { get; init; }
    public AvailabilityStatus? AvailabilityStatus { get; init; }

    // Connection status properties
    public required string ConnectionStatus { get; init; } // "pending" or "accepted"
    public DateTime? RequestedAt { get; init; }
    public string? RequestedBy { get; init; } // "user" or "coach"
    public string? Message { get; init; }
}

