using FluentAssertions;
using ProjectBrain.Auth.Auth0;
using ProjectBrain.Shared.Constants;

namespace ProjectBrain.Api.Tests;

public class Auth0UserManagementRolesCacheTests
{
    [Fact]
    public void BuildRolesCacheKey_IncludesDomain()
    {
        Auth0UserManagement.BuildRolesCacheKey("tenant.auth0.com")
            .Should().Be("Auth0ManagementApiRoles:tenant.auth0.com");
    }

    [Fact]
    public void BuildRoleUpdatePlan_ReportsMissingRequestedRoles()
    {
        var availableRoles = new[]
        {
            new Auth0Role { Id = "rol_user", Name = AppRoles.User }
        };

        var plan = Auth0UserManagement.BuildRoleUpdatePlan(
            availableRoles,
            [],
            [AppRoles.Admin]);

        plan.MissingRoles.Should().Equal(AppRoles.Admin);
        plan.RoleIdsToAssign.Should().BeEmpty();
        plan.RoleIdsToRemove.Should().BeEmpty();
    }

    [Fact]
    public void BuildRoleUpdatePlan_RequiresExactRoleNames()
    {
        var availableRoles = new[]
        {
            new Auth0Role { Id = "rol_admin", Name = "Admin" },
            new Auth0Role { Id = "rol_user", Name = AppRoles.User }
        };

        var plan = Auth0UserManagement.BuildRoleUpdatePlan(
            availableRoles,
            [new Auth0Role { Id = "rol_user", Name = AppRoles.User }],
            [AppRoles.Admin]);

        plan.MissingRoles.Should().Equal(AppRoles.Admin);
        plan.RoleIdsToAssign.Should().BeEmpty();
        plan.RoleIdsToRemove.Should().BeEmpty();
    }
}
