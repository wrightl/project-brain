using FluentAssertions;
using ProjectBrain.Shared.Constants;
using Xunit;

namespace ProjectBrain.Database.Tests;

public class AppRolesTests
{
    [Theory]
    [InlineData("user", true)]
    [InlineData("coach", true)]
    [InlineData("admin", true)]
    [InlineData("User", true)]
    [InlineData("ADMIN", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("superadmin", false)]
    public void IsValid_ReturnsExpected(string? role, bool expected)
    {
        AppRoles.IsValid(role).Should().Be(expected);
    }

    [Fact]
    public void All_ContainsCanonicalRoleNames()
    {
        AppRoles.All.Should().BeEquivalentTo([AppRoles.User, AppRoles.Coach, AppRoles.Admin]);
    }
}
