using FluentAssertions;
using ProjectBrain.Auth.Auth0;

namespace ProjectBrain.Api.Tests;

public class Auth0UserManagementRolesCacheTests
{
    [Fact]
    public void BuildRolesCacheKey_IncludesDomain()
    {
        Auth0UserManagement.BuildRolesCacheKey("tenant.auth0.com")
            .Should().Be("Auth0ManagementApiRoles:tenant.auth0.com");
    }
}
