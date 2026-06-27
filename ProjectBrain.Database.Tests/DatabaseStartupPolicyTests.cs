using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ProjectBrain.Database;

namespace ProjectBrain.Database.Tests;

public class DatabaseStartupPolicyTests
{
    [Theory]
    [InlineData("Development", null, true)]
    [InlineData("Production", "staging", false)]
    [InlineData("Production", "production", false)]
    [InlineData("Production", "STAGING", false)]
    [InlineData("Production", "qa", true)]
    public void ShouldRunMigrationsOnStartup_ReturnsExpected(
        string environmentName,
        string? deployEnv,
        bool expected)
    {
        var configData = new Dictionary<string, string?>();
        if (deployEnv != null)
        {
            configData["deploy-env"] = deployEnv;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var environment = new TestHostEnvironment(environmentName);

        DatabaseStartupPolicy.ShouldRunMigrationsOnStartup(configuration, environment)
            .Should()
            .Be(expected);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
