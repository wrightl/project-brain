using Aspire.Hosting.Pipelines;
using Azure.Provisioning.Search;

var builder = DistributedApplication.CreateBuilder(args);

// Parameters
var replicas = builder.AddParameter("minReplicas");
var useNewSearchService = (builder.Configuration["USE_NEW_SEARCH_SERVICE"] ?? "true").ToLower() == "true";

// Parameters - Azure AI Search
var existingSearchName = builder.AddParameter("searchName");
var existingAIResourceGroup = builder.AddParameter("searchResourceGroup");
// Parameters - Azure OpenAPI
var searchSku = builder.Configuration["AI_SEARCH_SKU"] ?? "Free";
var existingOpenAIName = builder.AddParameter("existingOpenAIName");
var chatModelName = builder.Configuration["CHAT_MODEL_NAME"] ?? "gpt-5-mini";
var chatModelVersion = builder.Configuration["CHAT_MODEL_VERSION"] ?? "2025-08-07";
var embedModelName = builder.Configuration["EMBED_MODEL_NAME"] ?? "text-embedding-3-small";
var embedModelVersion = builder.Configuration["EMBED_MODEL_VERSION"] ?? "1";
var modelSkuName = builder.Configuration["MODEL_SKU_NAME"] ?? "GlobalStandard";

// Secrets - these are used by the app when running locally and also in azure
var auth0ManagementApiClientSecret = builder.AddParameter("auth0-managementapiclientsecret", secret: true);
var auth0ManagementApiClientId = builder.AddParameter("auth0-managementapiclientid", secret: true);
var auth0ClientId = builder.AddParameter("auth0-clientid", secret: true);
var auth0Domain = builder.AddParameter("auth0-domain", secret: true);
var launchDarklySdkKey = builder.AddParameter("launchdarkly-sdk-key", secret: true);

// custom domain and certificate for container app - these are only needed for the deployment to azure
var certificateNameApiFromConfig = builder.Configuration["CERTIFICATE_NAME_API"] ?? "";
var certificateNameAppFromConfig = builder.Configuration["CERTIFICATE_NAME_APP"] ?? "";
var customDomainApiFromConfig = builder.Configuration["CUSTOMDOMAIN_API"] ?? "";
var customDomainAppFromConfig = builder.Configuration["CUSTOMDOMAIN_APP"] ?? "";
var customDomainApi = builder.AddParameter("customDomainApi", customDomainApiFromConfig, publishValueAsDefault: true);
var certificateNameApi = builder.AddParameter("certificateNameApi", value: certificateNameApiFromConfig, publishValueAsDefault: true);
var customDomainApp = builder.AddParameter("customDomainApp", customDomainAppFromConfig, publishValueAsDefault: true);
var certificateNameApp = builder.AddParameter("certificateNameApp", value: certificateNameAppFromConfig, publishValueAsDefault: true);

var search = builder.AddAzureSearch("search");
if (!useNewSearchService)
    search.RunAsExisting(existingSearchName, existingAIResourceGroup);
else
    search.ConfigureInfrastructure(infra =>
    {
        var searchService = infra.GetProvisionableResources()
                                 .OfType<SearchService>()
                                 .Single();

        searchService.SearchSkuName = Enum.Parse<SearchServiceSkuName>(searchSku);
    });

// Azure OpenAI
var openai = builder.AddAzureOpenAI("openai");
if (!useNewSearchService)
    openai.RunAsExisting(existingOpenAIName, existingAIResourceGroup);

// Chat deployment
openai.AddDeployment(
    name: "openai-chat-deployment",
    modelVersion: chatModelVersion,
    modelName: chatModelName)
    .WithProperties(deployment =>
    {
        deployment.SkuName = modelSkuName;
    });

// Embed deployment
openai.AddDeployment(
    name: "openai-embed-deployment",
    modelVersion: embedModelVersion,
    modelName: embedModelName)
    .WithProperties(deployment =>
    {
        deployment.SkuName = modelSkuName;
    });

// speech deployment
openai.AddDeployment(
    name: "openai-speech-deployment",
    modelVersion: "001",
    modelName: "whisper");

builder.AddBicepTemplate("speech", "Bicep/azureaispeech.bicep")
    .WithParameter("name", "myhealthdataservice");

var speechResource = builder.AddAzureContainerAppEnvironment("projectbrain-environment");
var speechConnectionString = speechResource.GetOutput("connectionString");

var cache = builder.AddRedis("cache")
        .PublishAsAzureContainerApp((module, app) =>
        {
            // Scale to 0
            app.Template.Scale.MinReplicas = replicas.AsProvisioningParameter(module);
        });

// api
var apiService = builder.AddProject<Projects.ProjectBrain_Api>("api")
                        .WithExternalHttpEndpoints()
                        .WithReference(search)
                        .WithReference(openai)
                        .WithReference(cache)
                        .WithEnvironment("ConnectionStrings__speech", speechConnectionString)
                        .WithEnvironment("Auth0__ManagementApiClientSecret", auth0ManagementApiClientSecret)
                        .WithEnvironment("Auth0__ManagementApiClientId", auth0ManagementApiClientId)
                        .WithEnvironment("Auth0__ClientId", auth0ClientId)
                        .WithEnvironment("Auth0__Domain", auth0Domain)
                        .WithEnvironment("LaunchDarkly__SdkKey", launchDarklySdkKey)
                        .WithEnvironment("AI__UseNewSearchService", useNewSearchService.ToString())
                        .WithHttpHealthCheck("/health")
                        .PublishAsAzureContainerApp((module, app) =>
                        {
                            // Scale to 0
                            app.Template.Scale.MinReplicas = replicas.AsProvisioningParameter(module);
#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                            app.ConfigureCustomDomain(customDomainApi, certificateNameApi);
#pragma warning restore ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        });

// azure storage
if (useNewSearchService)
{
    var documentStorage = builder.AddAzureStorage("documentstorage")
                                .RunAsEmulator(azurite =>
                                {
                                    azurite.WithDataVolume();
                                })
                                .AddBlobs("blobs");
    apiService.WithReference(documentStorage);
}
else
{
    var blobs = builder.AddConnectionString("blobs");
    apiService.WithReference(blobs);
}


var sqlServerName = "projectbrain";
var sqlDbName = "projectbraindb";

if (builder.ExecutionContext.IsPublishMode)
{
    // sql azure
    var azureSql = builder.AddAzureSqlServer(sqlServerName);

    var azureDb = azureSql.AddDatabase(sqlDbName);

    apiService.WithReference(azureDb)
              .WaitFor(azureDb);
}
else
{
    // sql server
    var sql = builder.AddSqlServer(sqlServerName)
        .WithLifetime(ContainerLifetime.Persistent);

    var db = sql.AddDatabase(sqlDbName);

    apiService.WithReference(db)
              .WaitFor(db);
}

if (builder.ExecutionContext.IsPublishMode)
{
    // Use Docker container for production
    var frontend = builder.AddDockerfile("frontend", "../projectbrain.frontend")
        .WaitFor(apiService)
        .WithReference(apiService)
        .WaitFor(cache)
        .WithHttpEndpoint(targetPort: 3000)
        .WithExternalHttpEndpoints()
        .PublishAsAzureContainerApp((module, app) =>
        {
            app.Template.Scale.MinReplicas = replicas.AsProvisioningParameter(module);
#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            app.ConfigureCustomDomain(customDomainApp, certificateNameApp);
#pragma warning restore ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        });
}
else
{
    // Use npm for development
    var frontend = builder.AddNpmApp("frontend", "../projectbrain.frontend", "dev")
        .WaitFor(apiService)
        .WithReference(apiService)
        .WaitFor(cache)
        .WithExternalHttpEndpoints();
}

// var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"];

// if (!builder.ExecutionContext.IsPublishMode && launchProfile == "https")
// {
//     frontend.RunWithHttpsDevCertificate("HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE");
// }

builder.Build().Run();
