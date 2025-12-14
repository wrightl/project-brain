using System.Text.Json;
using System.Text.Json.Serialization;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Shared.Dtos.Pagination;

public class UserManagementServices(
    ILogger<UserManagementServices> logger,
    IUserManagementService userManagementService,
    IUserService userService,
    IRoleManagement roleManagementService,
    IIdentityService identityService,
    IMemoryCache memoryCache,
    IConfiguration configuration)
{
    public ILogger<UserManagementServices> Logger { get; } = logger;
    public IUserManagementService UserManagementService { get; } = userManagementService;
    public IUserService UserService { get; } = userService;
    public IRoleManagement RoleManagementService { get; } = roleManagementService;
    public IIdentityService IdentityService { get; } = identityService;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public IConfiguration Configuration { get; } = configuration;
}

public static class UserManagement
{
    public static void MapUserManagementEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("usermanagement").RequireAuthorization("AdminOnly");

        // Admin-only endpoints
        group.MapGet("", GetAllUsers).WithName("GetAllUsers");
        group.MapGet("/{id}", GetUserById).WithName("GetUserById");
        // group.MapPut("/{id}", UpdateUser).WithName("UpdateUser");
        group.MapPut("{id}/roles", UpdateUserRoles).WithName("UpdateUserRoles");
        group.MapDelete("/{id}", DeleteUser).WithName("DeleteUser");
    }

    private static async Task<IResult> GetAllUsers([AsParameters] UserManagementServices services, HttpRequest request)
    {
        if (!services.IdentityService.IsAdmin)
        {
            throw new AppException("FORBIDDEN", "Admin access required", 403);
        }

        // Parse pagination parameters
        var pagedRequest = new PagedRequest();
        if (request.Query.TryGetValue("page", out var pageValue) &&
            int.TryParse(pageValue, out var page) && page > 0)
        {
            pagedRequest.Page = page;
        }
        if (request.Query.TryGetValue("pageSize", out var pageSizeValue) &&
            int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0)
        {
            pagedRequest.PageSize = pageSize;
        }

        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var (users, totalCount) = await services.UserManagementService.GetPaged(skip, take);

        var response = PagedResponse<BaseUserDto>.Create(pagedRequest, users, totalCount);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetUserById([AsParameters] UserManagementServices services, string id)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        var result = await services.UserService.GetById(id);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    // private static async Task<IResult> UpdateUser([AsParameters] UserManagementServices services, string id, UpdateUserRequest request)
    // {
    //     if (!services.IdentityService.IsAdmin)
    //     {
    //         return Results.Forbid();
    //     }

    //     var existingUser = await services.UserService.GetById(id);
    //     if (existingUser == null)
    //     {
    //         return Results.NotFound();
    //     }

    //     var userDto = new UserDto
    //     {
    //         Id = id,
    //         Email = existingUser.Email, // Email cannot be changed
    //         FullName = request.FullName ?? existingUser.FullName,
    //         IsOnboarded = request.IsOnboarded ?? existingUser.IsOnboarded,
    //         PreferredPronoun = request.PreferredPronoun ?? existingUser.PreferredPronoun,
    //         StreetAddress = request.StreetAddress ?? existingUser.StreetAddress,
    //         AddressLine2 = request.AddressLine2 ?? existingUser.AddressLine2,
    //         City = request.City ?? existingUser.City,
    //         StateProvince = request.StateProvince ?? existingUser.StateProvince,
    //         PostalCode = request.PostalCode ?? existingUser.PostalCode,
    //         Country = request.Country ?? existingUser.Country,
    //         Roles = existingUser.Roles // Roles are updated separately
    //     };

    //     var result = await services.UserService.Update(userDto);
    //     return Results.Ok(result);
    // }

    private static async Task<IResult> UpdateUserRoles([AsParameters] UserManagementServices services, [FromBody] UpdateUserRolesRequest request, string id)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        var existingUser = await services.UserService.GetById(id);
        if (existingUser == null)
        {
            return Results.NotFound();
        }

        // Update roles in database
        var result = await services.UserManagementService.UpdateRoles(id, request.Roles);

        // Update roles in Auth0
        await services.RoleManagementService.UpdateUserRoles(id, request.Roles);

        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteUser([AsParameters] UserManagementServices services, string id)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        var result = await services.UserService.DeleteById(id);

        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}

public class UpdateUserRequest
{
    public string? FullName { get; init; }
    public bool? IsOnboarded { get; init; }
    public string? PreferredPronoun { get; init; }
    public string? NeurodivergentDetails { get; init; }
    public string? StreetAddress { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? StateProvince { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
}

public class UpdateUserRolesRequest
{
    // public string help { get; init; }
    // [JsonPropertyName("roles")]
    public required List<string> Roles { get; init; }
}

