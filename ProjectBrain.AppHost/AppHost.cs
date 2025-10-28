var builder = DistributedApplication.CreateBuilder(args);

var replicas = builder.AddParameter("minReplicas", "0", false);

// custom domain and certificate for container app
var customDomain = builder.AddParameter("customDomain", "staging.dotanddashconsulting.com");
var certificateName = builder.AddParameter("certificateName", value: "staging.dotanddashconsulting-projectb-251015181848", publishValueAsDefault: true);

// Azure AI Search
var existingSearchName = builder.AddParameter("searchName", "projectbrain-poc");
var existingAIResourceGroup = builder.AddParameter("searchResourceGroup", "ai-resource-group");

var search = builder.AddAzureSearch("search")
                    .AsExisting(existingSearchName, existingAIResourceGroup);

// Azure OpenAI
var existingOpenAIName = builder.AddParameter("existingOpenAIName", "projectbrain-poc");
var openai = builder.AddAzureOpenAI("openai")
                    .AsExisting(existingOpenAIName, existingAIResourceGroup);

// Chat deployment
openai.AddDeployment(
    name: "openai-chat-deployment",
    modelName: "gpt-5-mini",
    modelVersion: "2025-08-07")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
    });

// Embed deployment
openai.AddDeployment(
    name: "openai-embed-deployment",
    modelName: "text-embedding-3-small",
    modelVersion: "1")
    .WithProperties(deployment =>
    {
        deployment.SkuName = "GlobalStandard";
    });

// azure storage
// var existingStorageName = builder.AddParameter("existingStorageName", "projectbraintempstorage");
// var storageaccount = builder.AddAzureStorage("storage")
//                     .AsExisting(existingStorageName, existingAIResourceGroup)
//                     .AddBlobs("resources");
var blobs = builder.AddConnectionString("blobs");

var containerAppEnvironment = builder.AddAzureContainerAppEnvironment("projectbrain-environment");

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
                        .WithReference(blobs)
                        .WithHttpHealthCheck("/health")
                        .PublishAsAzureContainerApp((module, app) =>
                        {
                            // app.Configuration.Ingress.External = true;
                            // Scale to 0
                            app.Template.Scale.MinReplicas = replicas.AsProvisioningParameter(module);
#pragma warning disable ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                            app.ConfigureCustomDomain(customDomain, certificateName);
#pragma warning restore ASPIREACADOMAINS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                        });

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
        .WithExternalHttpEndpoints();
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
