using ProjectBrain.AI;
using _shared = ProjectBrain.Models;
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

        group.MapGet("/", GetResources).WithName("GetResources");
        group.MapGet("/{filename}", GetResource).WithName("GetResource");
        group.MapGet("/{filename}/file", GetFile).WithName("GetFile");
        group.MapDelete("/{filename}", DeleteResource).WithName("DeleteResource");
        group.MapPost("/upload", UploadFiles).WithName("UploadFiles");
    }

    private static async Task<IResult> GetResources([AsParameters] ResourceServices services)
    {
        var user = await services.IdentityService.GetUserAsync();

        var resources = await services.ResourceService.GetAllForUser(user!.Id);
        return Results.Ok(resources);
    }

    private static async Task<IResult> GetResource(
        [AsParameters] ResourceServices services,
        string filename)
    {
        var user = await services.IdentityService.GetUserAsync();

        var resource = await services.ResourceService.GetById(
            Guid.Parse(filename), user!.Id);
        return resource is not null ? Results.Ok(resource) : Results.NotFound();
    }

    private static async Task<IResult> GetFile(
        [AsParameters] ResourceServices services,
        string filename)
    {
        var user = await services.IdentityService.GetUserAsync();

        var resource = await services.ResourceService.GetById(
            Guid.Parse(filename), user!.Id);
        if (resource is null)
            return Results.NotFound();

        var fileStream = await services.Storage.GetFile(resource.Location);
        if (fileStream is null)
            return Results.NotFound();

        return Results.File(fileStream, fileDownloadName: resource.FileName);
    }

    private static async Task<IResult> UploadFiles([AsParameters] ResourceServices services, HttpRequest request)
    {
        var form = await request.ReadFormAsync();

        // Get authenticated user from database
        var user = await services.IdentityService.GetUserAsync();
        if (user == null)
            return Results.Unauthorized();

        var userId = user.Id;

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
            var location = await services.Storage.UploadFile(file, userId, filename!);
            results.Add(new { status = "uploaded", filename, fileSize = file.Length, location });

            await services.ResourceService.Add(new Resource()
            {
                Id = Guid.NewGuid(),
                FileName = filename,
                Location = location,
                SizeInBytes = Convert.ToInt32(file.Length),
                UserId = userId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> DeleteResource([AsParameters] ResourceServices services, string filename)
    {
        var user = await services.IdentityService.GetUserAsync();

        var resource = await services.ResourceService.GetById(
            Guid.Parse(filename), user!.Id);

        if (resource is null)
            return Results.NotFound();

        await services.Storage.DeleteFile(resource.Location);
        await services.ResourceService.Remove(resource);
        return Results.Ok(filename);
    }
}