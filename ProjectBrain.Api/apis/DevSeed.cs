using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Database.Interfaces;

public static class DevSeedEndpoints
{
    public static void MapDevSeedEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        var group = app.MapGroup("dev/seed").WithTags("Dev Seed");

        group.MapPost("user", SeedUser)
            .WithName("SeedUserData")
            .Produces<SeedUserDataResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("coach", SeedCoach)
            .WithName("SeedCoachData")
            .Produces<SeedCoachDataResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("test-users", SeedTestUsers)
            .WithName("SeedTestUsers")
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<IResult> SeedTestUsers(
        [FromServices] ProjectBrainDbInitializer dbInitializer,
        [FromServices] IIdentitySeedingService identitySeedingService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        CancellationToken cancellationToken)
    {
        await dbInitializer.SeedTestUsersFromEndpointAsync(
            identitySeedingService,
            configuration,
            hostEnvironment,
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> SeedUser(
        [FromServices] IDevelopmentDataSeeder seeder,
        [FromBody] SeedUserRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "Email is required." });

        try
        {
            var result = await seeder.SeedUserDataAsync(request.Email.Trim(), cancellationToken);
            return Results.Ok(new SeedUserDataResponse(
                result.GoalsCreated,
                result.JournalEntriesCreated,
                result.TagsCreated));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> SeedCoach(
        [FromServices] IDevelopmentDataSeeder seeder,
        [FromBody] SeedCoachRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CoachUserEmail))
            return Results.BadRequest(new { error = "CoachUserEmail is required." });

        try
        {
            var result = await seeder.SeedCoachDataAsync(
                request.CoachUserEmail.Trim(),
                string.IsNullOrWhiteSpace(request.ClientUserEmail) ? null : request.ClientUserEmail.Trim(),
                cancellationToken);
            return Results.Ok(new SeedCoachDataResponse(
                result.CoachProfileCreated,
                result.QualificationsCreated,
                result.SpecialismsCreated,
                result.AgeGroupsCreated,
                result.ConnectionsCreated,
                result.CoachMessagesCreated));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public record SeedUserRequest(string Email);

public record SeedUserDataResponse(int GoalsCreated, int JournalEntriesCreated, int TagsCreated);

public record SeedCoachRequest(string CoachUserEmail, string? ClientUserEmail = null);

public record SeedCoachDataResponse(
    bool CoachProfileCreated,
    int QualificationsCreated,
    int SpecialismsCreated,
    int AgeGroupsCreated,
    int ConnectionsCreated,
    int CoachMessagesCreated);
