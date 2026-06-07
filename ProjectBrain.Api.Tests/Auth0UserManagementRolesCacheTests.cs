using FluentAssertions;
using ProjectBrain.Api.Authentication;

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
