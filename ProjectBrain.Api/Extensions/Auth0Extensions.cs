using Auth0.ManagementApi;
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Authentication;

public static class Auth0Extensions
{
    public static WebApplicationBuilder AddAuth0ManagementApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();

        builder.Services.AddScoped<IAuth0UserManagement, Auth0UserManagement>();
        builder.Services.AddScoped<Auth0UserManagementServices>();

        return builder;
    }
}