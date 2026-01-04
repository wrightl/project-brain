using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain.Mappers;
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
    ICoachRatingService coachRatingService)
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
}

public static class CoachEndpoints
{
    public static void MapCoachEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("coaches").RequireAuthorization();

        group.MapGet("/search", SearchCoaches).WithName("SearchCoaches");
        group.MapGet("/connected", GetConnectedCoaches).WithName("GetConnectedCoaches");
        group.MapGet("/clients", GetConnectedClients).WithName("GetConnectedClients").RequireAuthorization("CoachOnly");
        group.MapPost("/clients/{userId}/accept", AcceptClientConnection).WithName("AcceptClientConnection").RequireAuthorization("CoachOnly");

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

            // Create a dictionary to map coach IDs to connection status
            var connectionStatusMap = connectedCoaches.ToDictionary(c => c.Id, c => c.Status);

            // Fetch coach profiles for all connected coaches
            var coachProfiles = new List<CoachProfile>();
            foreach (var connection in connectedCoaches)
            {
                var coachProfile = await services.CoachProfileService.GetByUserId(connection.Id);
                if (coachProfile != null && coachProfile.User != null)
                {
                    coachProfiles.Add(coachProfile);
                }
            }

            // Convert to DTOs
            var coachDtos = coachProfiles
                .Select(cp => cp.ToCoachDto())
                .ToList();

            // Set online status for all coaches (30-minute window for coaches)
            await coachDtos.SetOnlineStatusAsync(services.UserActivityService, services.CoachMessageService, activityWindowMinutes: 30);

            // Create CoachWithConnectionStatusDto list with connection details
            var coachesWithStatus = new List<CoachWithConnectionStatusDto>();
            foreach (var coachDto in coachDtos)
            {
                // Get full connection details to access RequestedAt, RequestedBy, and Message
                var fullConnection = await services.ConnectionService.GetConnectionAsync(userId, coachDto.Id);

                coachesWithStatus.Add(new CoachWithConnectionStatusDto
                {
                    // BaseUserDto properties
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

                    // CoachDto specific properties
                    Qualifications = coachDto.Qualifications,
                    Specialisms = coachDto.Specialisms,
                    AgeGroups = coachDto.AgeGroups,
                    AvailabilityStatus = coachDto.AvailabilityStatus,

                    // Connection status properties
                    ConnectionStatus = connectionStatusMap.GetValueOrDefault(coachDto.Id, "pending"), // "pending" or "accepted"
                    RequestedAt = fullConnection?.RequestedAt ?? DateTime.UtcNow,
                    RequestedBy = fullConnection?.RequestedBy ?? "user",
                    Message = fullConnection?.Message,
                });
            }

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

            // Fetch user profiles for all connected users
            var clientDtos = new List<ClientWithConnectionStatusDto>();
            foreach (var connectionWithStatus in connections)
            {
                var user = await services.UserService.GetById(connectionWithStatus.Id) as UserDto;
                if (user != null)
                {
                    // Get full connection details to access RequestedAt, RequestedBy, and Message
                    var fullConnection = await services.ConnectionService.GetConnectionAsync(connectionWithStatus.Id, coachId);

                    // Load user profile for additional information
                    var userProfile = await services.UserProfileService.GetByUserId(connectionWithStatus.Id);

                    // Populate user profile data into UserDto
                    if (userProfile != null)
                    {
                        user.DoB = userProfile.DoB;
                        user.PreferredPronoun = userProfile.PreferredPronoun;
                        user.NeurodiverseTraits = userProfile.NeurodiverseTraits?.Select(t => t.Trait).ToList() ?? new List<string>();
                    }

                    // Get coach connection count
                    var connectedCoaches = await services.ConnectionService.GetConnectedCoachIdsAsync(connectionWithStatus.Id);
                    var coachesCount = connectedCoaches.Count;

                    // Get earliest connection date for time on platform calculation
                    var earliestConnectionDate = await services.ConnectionService.GetEarliestConnectionDateAsync(connectionWithStatus.Id);
                    TimeSpan? timeOnPlatform = null;
                    if (earliestConnectionDate.HasValue)
                    {
                        timeOnPlatform = DateTime.UtcNow - earliestConnectionDate.Value;
                    }

                    // Calculate age from DoB
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
                        ConnectionStatus = connectionWithStatus.Status, // "pending" or "accepted"
                        RequestedAt = fullConnection?.RequestedAt ?? DateTime.UtcNow,
                        RequestedBy = fullConnection?.RequestedBy ?? "user",
                        Message = fullConnection?.Message,
                        NeurodiverseTraits = user.NeurodiverseTraits ?? new List<string>(),
                        PreferredPronoun = user.PreferredPronoun,
                        Age = age,
                        ConnectedCoachesCount = coachesCount,
                        TimeOnPlatform = timeOnPlatform,
                    });
                }
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

    private static async Task<IResult> SearchCoaches(
        [AsParameters] CoachServices services,
        string? city = null,
        string? stateProvince = null,
        string? country = null,
        [FromQuery] string[]? ageGroups = null,
        [FromQuery] string[]? specialisms = null)
    {

        var coaches = await services.CoachProfileService.Search(
            city: city,
            stateProvince: stateProvince,
            country: country,
            ageGroups: ageGroups,
            specialisms: specialisms);

        var coachDtos = coaches
            .Where(cp => cp.User != null)
            .Select(cp => cp.ToCoachDto())
            .ToList();

        // Set online status for all coaches (30-minute window for coaches)
        await coachDtos.SetOnlineStatusAsync(services.UserActivityService, services.CoachMessageService, activityWindowMinutes: 30);

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
        await coachDto.SetOnlineStatusAsync(services.UserActivityService, services.CoachMessageService, activityWindowMinutes: 30);

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

        // Update user in database
        await services.UserService.Update(user);

        // Update coach profile if coach-specific fields are provided
        if (request.Qualifications != null || request.Specialisms != null || request.AgeGroups != null)
        {
            await services.CoachProfileService.CreateOrUpdate(
                userId,
                qualifications: request.Qualifications,
                specialisms: request.Specialisms,
                ageGroups: request.AgeGroups);
        }

        // Return the updated coach
        var updatedCoachProfile = await services.CoachProfileService.GetByUserId(userId);
        if (updatedCoachProfile == null || updatedCoachProfile.User == null)
        {
            return Results.NotFound();
        }

        var coachDto = updatedCoachProfile.ToCoachDto();

        // Set online status (30-minute window for coaches)
        await coachDto.SetOnlineStatusAsync(services.UserActivityService, services.CoachMessageService, activityWindowMinutes: 30);

        return Results.Ok(coachDto);
    }

    private static async Task<IResult> GetConnectionStatus(
        [AsParameters] CoachServices services,
        string id)
    {
        var userId = services.IdentityService.UserId!;

        // Validate coach exists
        var coachProfile = await services.CoachProfileService.GetByIdWithRelated(int.Parse(id));
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

        // Get connection
        var coachId = coachProfile.UserId;
        var connection = await services.ConnectionService.GetConnectionAsync(userId, coachId);

        if (connection == null || connection.Status == "cancelled" || connection.Status == "rejected")
        {
            return Results.Ok(new ConnectionStatusResponse
            {
                Status = "none"
            });
        }

        // Map internal status to API status
        string apiStatus = connection.Status switch
        {
            "pending" => "pending",
            "accepted" => "connected",
            _ => "none" // Should not reach here due to check above
        };

        var response = new ConnectionStatusResponse
        {
            Status = apiStatus,
            ConnectionId = connection.Id.ToString(),
            RequestedAt = connection.RequestedAt,
            RespondedAt = connection.RespondedAt,
            RequestedBy = connection.RequestedBy
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> SendConnectionRequest(
        [AsParameters] CoachServices services,
        string coachId,
        ConnectionRequestRequest? request)
    {
        var userId = services.IdentityService.UserId!;
        var user = await services.IdentityService.GetUserAsync();
        var isCoach = user?.Roles?.Any(r => r.ToLower() == "coach") ?? false;
        var userType = isCoach ? UserType.Coach : UserType.User;

        // Validate user cannot connect to themselves
        if (string.Equals(userId, coachId, StringComparison.OrdinalIgnoreCase))
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

        // Validate coach exists
        var coachProfile = await services.CoachProfileService.GetByUserId(coachId);
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

        // Check if connection already exists
        var existingConnection = await services.ConnectionService.GetConnectionAsync(userId, coachId);

        if (existingConnection != null)
        {
            // If connection exists and is pending or accepted, return it (idempotent)
            if (existingConnection.Status == "pending" || existingConnection.Status == "accepted")
            {
                var response = new ConnectionResponse
                {
                    Id = existingConnection.Id.ToString(),
                    Status = existingConnection.Status == "accepted" ? "connected" : "pending",
                    RequestedAt = existingConnection.RequestedAt,
                    CoachId = existingConnection.CoachId,
                    UserId = existingConnection.UserId
                };
                return Results.Ok(response);
            }
        }

        // Create connection request
        try
        {
            var connection = await services.ConnectionService.CreateConnectionRequestAsync(
                userId,
                coachId,
                UserType.User.ToString(),
                request?.Message);

            var response = new ConnectionResponse
            {
                Id = connection.Id.ToString(),
                Status = connection.Status == "accepted" ? "connected" : "pending",
                RequestedAt = connection.RequestedAt,
                CoachId = connection.CoachId,
                UserId = connection.UserId
            };

            return Results.Created($"/api/coaches/{coachId}/connections", response);
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

        // Get connection
        var connectionId = Guid.Parse(id);
        var connection = await services.ConnectionService.GetByIdAsync(connectionId);

        if (connection == null)
        {
            // Return success for idempotency - connection doesn't exist, so it's already "deleted"
            return Results.Ok(new { message = "Connection request cancelled or removed" });
        }

        // Authorization: Only the user who created the request can cancel it
        // OR if connected, either party can disconnect
        if (connection.Status == "pending" && !connection.RequestedBy.Equals(UserType.User.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            // If pending and requested by coach, user cannot cancel
            return Results.Forbid();
        }

        // Cancel or delete connection
        var success = await services.ConnectionService.CancelOrDeleteConnectionAsync(connectionId);

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
}

public class ConnectionRequestRequest
{
    public string? Message { get; init; }
}

public class SetAvailabilityStatusRequest
{
    public required string Status { get; init; } // "Available", "Busy", "Away", "Offline"
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

