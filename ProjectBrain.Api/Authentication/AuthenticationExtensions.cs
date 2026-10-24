using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ProjectBrain.Api.Hubs;
using ProjectBrain.Shared.Constants;

public static class AuthenticationExtensions
{
    public static void AddCustomAuthentication(this WebApplicationBuilder builder)
    {
        var audience = builder.Configuration["Auth0:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "Auth0:Audience must be configured for JWT validation.");
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}";
                options.Audience = audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = $"https://{builder.Configuration["Auth0:Domain"]}",
                    ValidAudience = audience,
                };

                // SignalR sends credentials in the query string. The web portal sends a short-lived
                // hub ticket; native clients send their access token.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async context =>
                    {
                        var accessToken = context.Request.Query["access_token"].ToString();
                        if (string.IsNullOrEmpty(accessToken) ||
                            !context.HttpContext.Request.Path.StartsWithSegments(CoachMessageHub.Path))
                        {
                            return;
                        }

                        if (!HubTicketService.IsTicket(accessToken))
                        {
                            context.Token = accessToken;
                            return;
                        }

                        var hubTicketService = context.HttpContext.RequestServices.GetRequiredService<IHubTicketService>();
                        var principal = await hubTicketService.RedeemAsync(accessToken, context.HttpContext.RequestAborted);
                        if (principal is null)
                        {
                            context.Fail("Invalid or expired hub ticket");
                            return;
                        }

                        context.Principal = principal;
                        context.Success();
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
                        policy.RequireClaim(AuthClaimTypes.Roles, AppRoles.Admin);
                    });
                options.AddPolicy("CoachOnly", policy =>
                    {
                        policy.RequireClaim(AuthClaimTypes.Roles, AppRoles.Coach);
                    });
                options.AddPolicy("UserOnly", policy =>
                    {
                        policy.RequireClaim(AuthClaimTypes.Roles, AppRoles.User);

                        // Make sure the user is not a coach
                        policy.RequireAssertion(context =>
                            {
                                return !context.User.HasClaim(c => c.Type == AuthClaimTypes.Roles && c.Value == AppRoles.Coach);
                            });
                    });
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