using Auth0.ManagementApi;
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Authentication;

public static class Auth0Extensions
{
    public static WebApplicationBuilder AddAuth0ManagementApi(this WebApplicationBuilder builder)
    {
        // 1. Add Memory Cache to store the Management API token
        builder.Services.AddMemoryCache();

        builder.Services.AddScoped<IRoleManagement, Auth0RoleManagement>();
        builder.Services.AddScoped<RoleManagementServices>();

        return builder;
    }
}