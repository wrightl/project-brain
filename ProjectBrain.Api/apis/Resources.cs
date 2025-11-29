using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class ResourceServices(ILogger<ResourceServices> logger,
    IConfiguration config,
    IResourceService resourceService,
    Storage storage,
    IIdentityService identityService)
{
    public ILogger<ResourceServices> Logger { get; } = logger;
    public IConfiguration Config { get; } = config;
    public IResourceService ResourceService { get; } = resourceService;
    public Storage Storage { get; } = storage;
    public IIdentityService IdentityService { get; } = identityService;
}

public static class ResourceEndpoints
{
    public static void MapResourceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("resource").RequireAuthorization();

        group.MapGet("/user", GetUserResources).WithName("GetUserResources");
        group.MapGet("/{id}/user", GetUserResource).WithName("GetUserResource");
        group.MapGet("/{id}/user/file", GetUserFile).WithName("GetUserFile");
        group.MapDelete("/{id}/user", DeleteUserResource).WithName("DeleteUserResource");
        group.MapPost("/upload/user", UploadUserFiles).WithName("UploadUserFiles");
        group.MapPost("/reindex/user", ReindexUserResources).WithName("ReindexUserResources");

        group.MapGet("/shared", GetSharedResources).WithName("GetSharedResources");
        group.MapGet("/{id}/shared", GetSharedResource).WithName("GetSharedResource");
        group.MapGet("/{id}/shared/file", GetSharedFile).WithName("GetSharedFile");
        group.MapDelete("/{id}/shared", DeleteSharedResource).WithName("DeleteSharedResource");
        group.MapPost("/upload/shared", UploadSharedFiles).WithName("UploadSharedFiles");
        group.MapPost("/reindex/shared", ReindexSharedResources).WithName("ReindexSharedResources");
    }

    private static async Task<IResult> GetUserResources([AsParameters] ResourceServices services)
    {
        var userId = services.IdentityService.UserId;

        var resources = await services.ResourceService.GetAllForUser(userId!);
        return Results.Ok(resources);
    }

    private static async Task<IResult> GetSharedResources([AsParameters] ResourceServices services)
    {
        var resources = await services.ResourceService.GetAllShared();
        return Results.Ok(resources);
    }

    private static async Task<IResult> GetUserResource(
        [AsParameters] ResourceServices services,
        string id)
    {
        var userId = services.IdentityService.UserId;

        var resource = await services.ResourceService.GetForUserById(
            Guid.Parse(id), userId!);
        return resource is not null ? Results.Ok(resource) : Results.NotFound();
    }

    private static async Task<IResult> GetSharedResource(
        [AsParameters] ResourceServices services,
        string id)
    {
        var resource = await services.ResourceService.GetSharedById(
            Guid.Parse(id));
        return resource is not null ? Results.Ok(resource) : Results.NotFound();
    }

    private static async Task<IResult> GetUserFile(
        [AsParameters] ResourceServices services,
        string id)
    {
        var user = await services.IdentityService.GetUserAsync();

        var resource = await services.ResourceService.GetForUserById(
            Guid.Parse(id), user!.Id);
        if (resource is null)
            return Results.NotFound();

        var fileStream = await services.Storage.GetFile(resource.Location);
        if (fileStream is null)
            return Results.NotFound();

        return Results.File(fileStream, fileDownloadName: resource.FileName, contentType: "application/octet-stream");
    }

    private static async Task<IResult> GetSharedFile(
        [AsParameters] ResourceServices services,
        string id)
    {
        var resource = await services.ResourceService.GetSharedById(
            Guid.Parse(id));
        if (resource is null)
            return Results.NotFound();

        var fileStream = await services.Storage.GetFile(resource.Location);
        if (fileStream is null)
            return Results.NotFound();

        return Results.File(fileStream, fileDownloadName: resource.FileName, contentType: "application/octet-stream");
    }

    private static async Task<IResult> UploadUserFiles([AsParameters] ResourceServices services, HttpRequest request)
    {
        // Get authenticated user from database
        var userId = services.IdentityService.UserId;
        return await uploadFiles(services, request, userId);
    }

    private static async Task<IResult> UploadSharedFiles([AsParameters] ResourceServices services, HttpRequest request)
    {
        return await uploadFiles(services, request);
    }

    private static async Task<IResult> uploadFiles([AsParameters] ResourceServices services, HttpRequest request, string? userId = null)
    {
        var form = await request.ReadFormAsync();

        if (form.Files.Count == 0)
            return Results.BadRequest("No files uploaded");

        var results = new List<object>();
        foreach (var file in form.Files)
        {
            var filename = form.TryGetValue("filename", out var fn) ? fn.ToString() : file?.FileName;
            if (file == null || file.Length == 0)
            {
                results.Add(new { status = "error", filename, message = "File is empty" });
                continue;
            }
            if (string.IsNullOrEmpty(filename))
            {
                results.Add(new { status = "error", filename = "unknown", message = "Filename is required" });
                continue;
            }

            // Check if resource already exists in database
            var existingResource = (userId is null
                                    ? await services.ResourceService.GetSharedByFilename(filename)
                                    : await services.ResourceService.GetForUserByFilename(filename, userId!));
            if (existingResource is not null)
            {
                results.Add(new { status = "error", filename, message = "File already exists" });
                continue;
            }

            var resourceId = Guid.NewGuid();
            var location = await services.Storage.UploadFile(file, filename, userId, resourceId.ToString());
            results.Add(new { status = "uploaded", filename, fileSize = file.Length, location });

            await services.ResourceService.Add(new Resource()
            {
                Id = resourceId,
                FileName = filename,
                Location = location,
                SizeInBytes = Convert.ToInt32(file.Length),
                UserId = userId ?? string.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsShared = userId is null ? true : false
            });
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> DeleteUserResource([AsParameters] ResourceServices services, string id)
    {
        var userId = services.IdentityService.UserId!;
        var resource = await services.ResourceService.GetForUserById(
            Guid.Parse(id), userId);

        if (resource is null)
            return Results.NotFound();

        return await DeleteResource(services, resource);
    }

    private static async Task<IResult> DeleteSharedResource([AsParameters] ResourceServices services, string id)
    {
        var resource = await services.ResourceService.GetSharedById(
            Guid.Parse(id));

        if (resource is null)
            return Results.NotFound();
        return await DeleteResource(services, resource);
    }

    private static async Task<IResult> DeleteResource([AsParameters] ResourceServices services, Resource resource)
    {
        await services.Storage.DeleteFile(resource.Location);
        await services.ResourceService.Remove(resource);
        return Results.Ok(resource.Id);
    }

    private static async Task<IResult> ReindexUserResources([AsParameters] ResourceServices services)
    {
        var userId = services.IdentityService.UserId;

        return await ReindexResources(services, userId!);
    }

    private static async Task<IResult> ReindexSharedResources([AsParameters] ResourceServices services)
    {
        return await ReindexResources(services, null);
    }

    private static async Task<IResult> ReindexResources([AsParameters] ResourceServices services, string? userId)
    {
        try
        {
            var result = await services.Storage.ReindexFiles(services.ResourceService, userId);
            return Results.Ok(new { status = "success", filesReindexed = result });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error reindexing files for user {UserId}", userId);
            return Results.Problem($"Error reindexing files: {ex.Message}");
        }
    }
}