using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Tests;

public class FakeCoachEnvironmentTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IsEnabled_ReturnsConfigurationValue(bool enabled, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FakeCoachAutoReply:Enabled"] = enabled.ToString(),
            })
            .Build();

        FakeCoachEnvironment.IsEnabled(configuration).Should().Be(expected);
    }
}
