
using ProjectBrain.Api.Authentication;

public class UserServices(
    ILogger<UserServices> logger,
    IIdentityService identityService,
    IUserService userService)
{
    public ILogger<UserServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IUserService UserService { get; } = userService;
}

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("users").RequireAuthorization();

        group.MapPost("/me/onboarding", OnboardUser);
        group.MapGet("/me", GetCurrentUser).WithName("GetCurrentUser");
        group.MapGet("/{email}", GetUserByEmail).WithName("GetUserByEmail");
    }

    private static async Task<IResult> OnboardUser([AsParameters] UserServices services, CreateUserRequest request)
    {
        var userId = services.IdentityService.UserId!;
        var user = new User()
        {
            Id = userId,
            Email = request.Email,
            FullName = request.FullName,
            DoB = request.DoB,
            FavoriteColor = request.FavoriteColor,
            IsOnboarded = true
        };
        var result = await services.UserService.Create(user);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCurrentUser([AsParameters] UserServices services)
    {
        var userId = services.IdentityService.UserId;
        var result = await services.UserService.GetById(userId!);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> GetUserByEmail([AsParameters] UserServices services, string email)
    {
        var result = await services.UserService.GetByEmail(email);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}

public class CreateUserRequest
{
    public required string Email { get; init; }
    public required string FullName { get; init; }

    public required DateOnly DoB { get; init; }
    public required string FavoriteColor { get; init; }
}