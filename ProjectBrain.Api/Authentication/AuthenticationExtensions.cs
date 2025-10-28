using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

public static class AuthenticationExtensions
{
    public static void AddCustomAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
                options.Audience = "dynamic-audience-placeholder"; //$"{builder.Configuration["Auth0:Audience"]}";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // NameClaimType = ClaimTypes.NameIdentifier,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = $"https://{builder.Configuration["Auth0:Domain"]}/",
                    AudienceValidator = (audiences, securityToken, validationParameters) =>
                    {
                        var httpContext = builder.Services.BuildServiceProvider().GetRequiredService<IHttpContextAccessor>().HttpContext;
                        var requestedAudience = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}";
                        var clientId = builder.Configuration["Auth0:ClientId"];

                        // Accept either the API audience (access token) or the client ID (ID token)
                        return audiences.Contains(requestedAudience) || audiences.Contains(clientId);
                    }
                };
            });

        // // Prevent mapping "sub" claim to nameidentifier.
        // JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");
    }

    public static void AddCustomAuthorisation(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization();
        // builder.Services.AddAuthorization(options =>
        //     {
        //         options.AddPolicy("read:weather_forecast", policy =>
        //             {
        //                 // policy.Requirements.Add(new RbacRequirement("read:weather_forecast"));
        //                 policy.RequireClaim("scope", "read:weather_forecast");
        //             });
        //     });

        // builder.Services.AddSingleton<IAuthorizationHandler, RbacHandler>();
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