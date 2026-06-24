using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDatabase_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:projectbraindb"] = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;",
                ["ConnectionStrings:azurecache"] = "localhost:6379",
                ["ConnectionStrings:blobs"] = "UseDevelopmentStorage=true",
                ["ConnectionStrings:openai"] = "Endpoint=https://example.openai.azure.com/;Key=fake",
                ["ConnectionStrings:ai-search"] = "Endpoint=https://example.search.windows.net;Key=fake",
                ["Aspire:Microsoft:EntityFrameworkCore:SqlServer:DisableHealthChecks"] = "true",
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "",
                ["OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY"] = "none",
                ["PushNotifications:Enabled"] = "false",
                ["Auth0:Domain"] = "test.auth0.com",
                ["Auth0:Audience"] = "https://test-api",
                ["Auth0:ClientId"] = "test-client",
                ["Auth0:ClientSecret"] = "test-secret",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IAuth0UserManagement>();
            services.AddSingleton<IAuth0UserManagement, FakeAuth0UserManagement>();
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
            services.RemoveAll<BlobServiceClient>();
            services.AddSingleton(_ => new BlobServiceClient("UseDevelopmentStorage=true"));
            services.RemoveAll<ISearchIndexService>();
            services.AddSingleton<ISearchIndexService, FakeSearchIndexService>();
            services.RemoveAll<IUserMemoryIndexService>();
            services.AddScoped<IUserMemoryIndexService, NoOpUserMemoryIndexService>();
            services.RemoveAll<IUserMemoryRetrievalService>();
            services.AddScoped<IUserMemoryRetrievalService, SqlUserMemoryRetrievalService>();

            var hostedServicesToRemove = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType == typeof(ProjectBrainDbInitializer))
                .ToList();
            foreach (var descriptor in hostedServicesToRemove)
            {
                services.Remove(descriptor);
            }

            var healthCheckDescriptors = services
                .Where(d => d.ServiceType == typeof(IHealthCheck) &&
                            d.ImplementationType == typeof(DatabaseMigrationsHealthCheck))
                .ToList();
            foreach (var descriptor in healthCheckDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var logger = sp.GetRequiredService<ILogger<AppDbContext>>();
                options.UseInMemoryDatabase(_databaseName);
            });
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }
}

internal sealed class FakeSearchIndexService : ISearchIndexService
{
    public Task<Response<SearchResults<SearchDocument>>> SearchAsync(string query, SearchOptions searchOptions) =>
        throw new NotSupportedException("Search is disabled in integration tests.");

    public Task DeleteDocumentsFromIndexAsync(string filename, string location) => Task.CompletedTask;

    public Task DeleteAllDocumentsFromIndexAsync(string? userId) => Task.CompletedTask;

    public Task<int> DeleteAllDocumentsForUserAsync(string? userId) => Task.FromResult(0);

    public Task ExtractEmbedAndIndexFromStreamAsync(
        Stream stream,
        string filename,
        string? userId,
        string blobPath,
        string resourceId,
        bool removeExistingDocuments = false) => Task.CompletedTask;
}

internal sealed class FakeAuth0UserManagement : IAuth0UserManagement
{
    public Task<string?> CreateUser(string email, string password, string fullName, string connection, bool emailVerified) =>
        Task.FromResult<string?>("test-user-123");

    public Task<bool> UpdateUserRoles(string userId, List<string> roles) =>
        Task.FromResult(true);

    public Task<bool> UpdateUser(string userId, BaseUserDto user) =>
        Task.FromResult(true);

    public Task<bool> DeleteUserById(string id) =>
        Task.FromResult(true);

    public Task<string?> GetUserIdByEmail(string email) =>
        Task.FromResult<string?>(null);
}
