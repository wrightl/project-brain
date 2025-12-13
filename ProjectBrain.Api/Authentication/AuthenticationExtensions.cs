using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

public static class AuthenticationExtensions
{
    public static void AddCustomAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
                // options.Audience = $"{builder.Configuration["Auth0:Audience"]}";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // NameClaimType = ClaimTypes.NameIdentifier,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = $"https://{builder.Configuration["Auth0:Domain"]}",
                    AudienceValidator = (audiences, securityToken, validationParameters) =>
                    {
                        var httpContext = builder.Services.BuildServiceProvider().GetRequiredService<IHttpContextAccessor>().HttpContext;
                        var requestedAudience = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}";
                        var clientId = builder.Configuration["Auth0:ClientId"];

                        // Accept either the API audience (access token) or the client ID (ID token)
                        return audiences.Contains(requestedAudience) || audiences.Contains(clientId);
                    }
                };

                // Handle SignalR connections - extract token from query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for a SignalR hub, get the token from the query string
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        // // Prevent mapping "sub" claim to nameidentifier.
        // JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");
    }

    public static void AddCustomAuthorisation(this WebApplicationBuilder builder)
    {
        // builder.Services.AddAuthorization();
        builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    {
                        policy.RequireClaim("https://projectbrain.app/roles", "admin");
                    });
                options.AddPolicy("CoachOnly", policy =>
                    {
                        policy.RequireClaim("https://projectbrain.app/roles", "coach");
                    });
                // options.AddPolicy("UserOnly", policy =>
                //     {
                //         policy.RequireClaim("https://projectbrain.app/roles", "user");
                //     });
            });
    }

    public static void UseCustomAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
    }

    public static void UseCustomAuthorisation(this WebApplication app)
    {
        app.UseAuthorization();
    }
}

class RbacRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public RbacRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

class RbacHandler : AuthorizationHandler<RbacRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RbacRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == "permissions"))
        {
            return Task.CompletedTask;
        }

        var permission = context.User.FindFirst(c => c.Type == "permissions" && c.Value == requirement.Permission);

        if (permission == null)
        {
            return Task.CompletedTask;
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}