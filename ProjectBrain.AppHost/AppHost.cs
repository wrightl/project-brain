using Aspire.Hosting.Pipelines;
using Azure.Core;
using Azure.Provisioning.Expressions;
using ProjectBrain.AppHost;
using Azure.Provisioning.AppConfiguration;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Search;
using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

var appName = "projectbrain";
var apiName = "api";
var frontendName = "frontend";
var documentstorageName = "documentstorage";
var cacheName = "cache";
var searchName = "ai-search";
var openaiName = "openai";
var speechName = "speech";
var speechRegion = "westeurope";
var sqlServerName = $"{appName}";
var sqlDbName = $"{appName}db";
var defaultSearchSku = "Free";
var defaultChatModelName = "gpt-5-mini";
var defaultChatModelVersion = "2025-08-07";
var defaultEmbedModelName = "text-embedding-3-small";
var defaultEmbedModelVersion = "1";
var defaultModelSkuName = "GlobalStandard";
var blobName = "blobs";

// Parameters
var environmentName = builder.AddParameter("deploy-env");
var replicas = builder.AddParameter("minReplicas");
var sqlPassword = builder.AddParameter($"{sqlServerName}-password", secret: true);

// Parameters - Azure AI Search
var searchSku = builder.Configuration["AI_SEARCH_SKU"] ?? defaultSearchSku;
var chatModelName = builder.Configuration["CHAT_MODEL_NAME"] ?? defaultChatModelName;
var chatModelVersion = builder.Configuration["CHAT_MODEL_VERSION"] ?? defaultChatModelVersion;
var embedModelName = builder.Configuration["EMBED_MODEL_NAME"] ?? defaultEmbedModelName;
var embedModelVersion = builder.Configuration["EMBED_MODEL_VERSION"] ?? defaultEmbedModelVersion;
var modelSkuName = builder.Configuration["MODEL_SKU_NAME"] ?? defaultModelSkuName;

// Secrets - these are used by the app when running locally and also in azure
var auth0ManagementApiClientSecret = builder.AddParameter("auth0-managementapiclientsecret", secret: true);
var auth0ManagementApiClientId = builder.AddParameter("auth0-managementapiclientid", secret: true);
var auth0ClientId = builder.AddParameter("auth0-clientid", secret: true);
var auth0Domain = builder.AddParameter("auth0-domain", secret: true);
var auth0WebhookToken = builder.AddParameter("auth0-webhook-token", secret: true);
var launchDarklySdkKey = builder.AddParameter("launchdarkly-sdk-key", secret: true);
var mailgunApiKey = builder.AddParameter("mailgun-api-key", secret: true);
var mailgunDomain = builder.AddParameter("mailgun-domain", secret: true);
var firebaseCredentialsJson = builder.AddParameter("firebase-credentials-json", secret: true);
var adminUserPassword = builder.AddParameter("admin-user-password", secret: true);
var googleMapsGeocodingApiKey = builder.AddParameter("google-maps-geocoding-api-key", secret: true);

// custom domain and certificate for container app - these are only needed for the deployment to azure
var certificateNameApiFromConfig = builder.Configuration["CERTIFICATE_NAME_API"] ?? "";
var certificateNameAppFromConfig = builder.Configuration["CERTIFICATE_NAME_APP"] ?? "";
var customDomainApiFromConfig = builder.Configuration["CUSTOMDOMAIN_API"] ?? "";
var customDomainAppFromConfig = builder.Configuration["CUSTOMDOMAIN_APP"] ?? "";
var customDomainApi = builder.AddParameter("customDomainApi", customDomainApiFromConfig, publishValueAsDefault: true);
var certificateNameApi = builder.AddParameter("certificateNameApi", value: certificateNameApiFromConfig, publishValueAsDefault: true);
var customDomainApp = builder.AddParameter("customDomainApp", customDomainAppFromConfig, publishValueAsDefault: true);
var certificateNameApp = builder.AddParameter("certificateNameApp", value: certificateNameAppFromConfig, publishValueAsDefault: true);

var search = builder.AddAzureSearch(searchName);
search.ConfigureInfrastructure(infra =>
{
    var searchService = infra.GetProvisionableResources()
                             .OfType<SearchService>()
                             .Single();

    searchService.SearchSkuName = Enum.Parse<SearchServiceSkuName>(searchSku);
});

// Azure OpenAI
var openai = builder.AddAzureOpenAI(openaiName).ConfigureInfrastructure(infra =>
{
    var openaiService = infra.GetProvisionableResources()
                             .OfType<CognitiveServicesAccount>()
                             .Single();
    openaiService.Location = new AzureLocation(speechRegion);
});

// Chat deployment
openai.AddDeployment(
    name: $"{openaiName}-chat-deployment",
    modelVersion: chatModelVersion,
    modelName: chatModelName)
    .WithProperties(deployment =>
    {
        deployment.SkuName = modelSkuName;
    });

// Embed deployment
openai.AddDeployment(
    name: $"{openaiName}-embed-deployment",
    modelVersion: embedModelVersion,
    modelName: embedModelName)
    .WithProperties(deployment =>
    {
        deployment.SkuName = modelSkuName;
    });

var speechResource = builder.AddBicepTemplate(speechName, "Bicep/azureaispeech.bicep")
    .WithParameter("name", speechName)
    .WithParameter("location", speechRegion);
var speechConnectionString = speechResource.GetOutput("connectionString");

// speech deployment
openai.AddDeployment(
    name: $"{openaiName}-{speechName}-deployment",
    modelVersion: "001",
    modelName: "whisper")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "Standard"; // Whisper uses Standard, not GlobalStandard
        deployment.SkuCapacity = 3; // Match your quota limit
    });

// App Container Environment
builder.AddAzureContainerAppEnvironment($"{appName}-environment");

// azure app config
var appConfig = builder.AddAzureAppConfiguration("config");
if (!builder.ExecutionContext.IsPublishMode)
{
    appConfig.RunAsEmulator(emulator =>
    {
        emulator.WithDataVolume();
        emulator.WithHostPort(54607);
    });
}
else
{
    appConfig.ConfigureInfrastructure(infra =>
    {
        var appConfigStore = infra.GetProvisionableResources()
                                  .OfType<AppConfigurationStore>()
                                  .Single();

        appConfigStore.SkuName = "Free";
        appConfigStore.EnablePurgeProtection = false;
    });
}

// Azure Managed Redis in cloud; local Redis container for development (Aspire 13.1+)
var cache = builder.AddAzureManagedRedis(cacheName)
    .WithAccessKeyAuthentication()
    .RunAsContainer();

// api
var apiService = builder.AddProject<Projects.ProjectBrain_Api>(apiName)
                        .WithExternalHttpEndpoints()
                        .WithReference(search)
                        .WithReference(openai)
                        .WithReference(cache)
                        .WithReference(appConfig)
                        // .WithEnvironment("ConnectionStrings__speech", speechConnectionString)
                        .WithEnvironment("Auth0__ManagementApiClientSecret", auth0ManagementApiClientSecret)
                        .WithEnvironment("Auth0__ManagementApiClientId", auth0ManagementApiClientId)
                        .WithEnvironment("Auth0__ClientId", auth0ClientId)
                        .WithEnvironment("Auth0__Domain", auth0Domain)
                        .WithEnvironment("Auth0__WebhookToken", auth0WebhookToken)
                        .WithEnvironment("Firebase__CredentialsJson", firebaseCredentialsJson)
                        .WithEnvironment("LaunchDarkly__SdkKey", launchDarklySdkKey)
                        .WithEnvironment("Mailgun__ApiKey", mailgunApiKey)
                        .WithEnvironment("Mailgun__Domain", mailgunDomain)
                        .WithEnvironment("AdminUser__Password", adminUserPassword)
                        .WithEnvironment("GoogleMaps__GeocodingApiKey", googleMapsGeocodingApiKey)
                        .WithHttpHealthCheck("/alive")
                        .PublishAsAzureContainerApp((module, app) =>
                        {
                            // Scale to 0
                            app.Template.Scale.MinReplicas = replicas.AsProvisioningParameter(module);
                            ContainerAppResourceDefaults.ApplyQuarterCoreHalfGi(app);
#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                            app.ConfigureCustomDomain(customDomainApi, certificateNameApi);
#pragma warning restore ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        });

// azure storage (private: no public blob access when deployed to Azure)
var documentStorage = builder.AddAzureStorage(documentstorageName);
documentStorage.RunAsEmulator(azurite =>
{
    azurite.WithDataVolume();
});
documentStorage.ConfigureInfrastructure(infra =>
{
    var storageAccount = infra.GetProvisionableResources()
                              .OfType<StorageAccount>()
                              .Single();
    storageAccount.AllowBlobPublicAccess = false;
});
var documentBlobs = documentStorage.AddBlobs(blobName);
var documentQueues = documentStorage.AddQueues("queues");
apiService.WithReference(documentBlobs);
apiService.WithReference(documentQueues);

if (builder.ExecutionContext.IsPublishMode)
{
    // variables and secrets only needed for the deployment to azure
    // variables
    var appBaseUrl = builder.AddParameter("app-base-url");
    var auth0Audience = builder.AddParameter("auth0-audience");
    // var auth0Scope = builder.AddParameter("auth0-scope", value: "", publishValueAsDefault: true);
    var apiServerUrl = builder.AddParameter("api-server-url");

    // secrets
    var auth0Secret = builder.AddParameter("auth0-secret", secret: true);
    var auth0ClientSecret = builder.AddParameter("auth0-client-secret", secret: true);
    var nextPublicLaunchDarklyClientId = builder.AddParameter("next-public-launchdarkly-client-id", secret: true);
    var googleMapsApiKey = builder.AddParameter("next-public-google-maps-api-key", secret: true);

    // Grafana Cloud OTLP (optional): set Parameters__otel_exporter_otlp_endpoint and Parameters__otel_exporter_otlp_headers in CI to enable
    var otelOtlpEndpoint = builder.AddParameter("otel-exporter-otlp-endpoint");
    var otelOtlpHeaders = builder.AddParameter("otel-exporter-otlp-headers", secret: true);
    var otelResourceAttributes = builder.AddParameter("otel-resource-attributes");

    // sql azure
    var azureSql = builder.AddAzureSqlServer(sqlServerName);

    var azureDb = azureSql.AddDatabase(sqlDbName);

    apiService.WithReference(azureDb)
              .WaitFor(azureDb)
              .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelOtlpEndpoint)
              .WithEnvironment("OTEL_EXPORTER_OTLP_HEADERS", otelOtlpHeaders)
              .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
              .WithEnvironment("OTEL_SERVICE_NAME", "projectbrain-api")
              .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", otelResourceAttributes);

    // Use Docker container for production frontend
    // Pass DEPLOY_ENV as build argument to select the correct .env file
    var frontend = builder.AddDockerfile(frontendName, $"../{appName}.{frontendName}")
        .WithBuildArg("DEPLOY_ENV", environmentName)
        .WithEnvironment("APP_BASE_URL", appBaseUrl)
        .WithEnvironment("AUTH0_SECRET", auth0Secret)
        .WithEnvironment("AUTH0_DOMAIN", auth0Domain)
        .WithEnvironment("AUTH0_CLIENT_ID", auth0ClientId)
        .WithEnvironment("AUTH0_CLIENT_SECRET", auth0ClientSecret)
        .WithEnvironment("AUTH0_AUDIENCE", auth0Audience)
        // .WithEnvironment("AUTH0_SCOPE", auth0Scope)
        .WithEnvironment("API_SERVER_URL", apiServerUrl)
        .WithEnvironment("LAUNCHDARKLY_SDK_KEY", launchDarklySdkKey)
        .WithEnvironment("NEXT_PUBLIC_LAUNCHDARKLY_CLIENT_ID", nextPublicLaunchDarklyClientId)
        .WithEnvironment("GOOGLE_MAPS_GEOCODING_API_KEY", googleMapsGeocodingApiKey)
        .WithEnvironment("NEXT_PUBLIC_GOOGLE_MAPS_API_KEY", googleMapsApiKey)
        .WithReference(apiService)
        .WaitFor(apiService)
        .WaitFor(cache)
        .WithHttpEndpoint(targetPort: 3000)
        .WithExternalHttpEndpoints()
        .PublishAsAzureContainerApp((module, app) =>
        {
            app.Template.Scale.MinReplicas = replicas.AsProvisioningParameter(module);
            ContainerAppResourceDefaults.ApplyQuarterCoreHalfGi(app);
#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            app.ConfigureCustomDomain(customDomainApp, certificateNameApp);
#pragma warning restore ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        });
}
else
{
    // Create a devtunnel
    builder.AddDevTunnel("tunnel")
        .WithReference(apiService)
        .WithAnonymousAccess();

    // sql server
    var sql = builder.AddSqlServer(sqlServerName, password: sqlPassword, port: 49976)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume();

    var db = sql.AddDatabase(sqlDbName);

    apiService.WithReference(db)
              .WaitFor(db);


    // Use npm for frontend development
    var frontend = builder.AddNpmApp(frontendName, $"../{appName}.{frontendName}", "dev")
        .WithReference(apiService)
        .WaitFor(apiService)
        .WaitFor(cache)
        .WithExternalHttpEndpoints();
}

// var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];

// if (!builder.ExecutionContext.IsPublishMode && launchProfile == "https")
// {
//     frontend.RunWithHttpsDevCertificate("HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE");
// }

builder.Build().Run();
