using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Pagination;

public class ResourceServices(ILogger<ResourceServices> logger,
    IConfiguration config,
    IResourceService resourceService,
    IResourceRepository resourceRepository,
    Storage storage,
    IIdentityService identityService,
    IFeatureGateService featureGateService,
    ISubscriptionService subscriptionService,
    IUsageTrackingService usageTrackingService)
{
    public ILogger<ResourceServices> Logger { get; } = logger;
    public IConfiguration Config { get; } = config;
    public IResourceService ResourceService { get; } = resourceService;
    public IResourceRepository ResourceRepository { get; } = resourceRepository;
    public Storage Storage { get; } = storage;
    public IIdentityService IdentityService { get; } = identityService;
    public IFeatureGateService FeatureGateService { get; } = featureGateService;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
    public IUsageTrackingService UsageTrackingService { get; } = usageTrackingService;
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

    private static async Task<IResult> GetUserResources(
        [AsParameters] ResourceServices services,
        HttpRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        // Parse pagination parameters
        var pagedRequest = new PagedRequest();
        if (request.Query.TryGetValue("page", out var pageValue) &&
            int.TryParse(pageValue, out var page) && page > 0)
        {
            pagedRequest.Page = page;
        }

        if (request.Query.TryGetValue("pageSize", out var pageSizeValue) &&
            int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0)
        {
            pagedRequest.PageSize = pageSize;
        }

        // Get total count for pagination
        var totalCount = await services.ResourceRepository.CountForUserAsync(userId, CancellationToken.None);

        // Get paginated results using efficient database-level pagination
        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var paginatedResources = await services.ResourceRepository.GetPagedForUserAsync(userId, skip, take, CancellationToken.None);

        var response = PagedResponse<Resource>.Create(pagedRequest, paginatedResources, totalCount);
        return Results.Ok(response);
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

        var options = new StorageOptions
        {
            UserId = user!.Id,
            FileOwnership = FileOwnership.User,
            StorageType = StorageType.Resources
        };
        var fileStream = await services.Storage.GetFile(resource.FileName, options);
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

        var options = new StorageOptions
        {
            FileOwnership = FileOwnership.Shared,
            StorageType = StorageType.Resources
        };
        var fileStream = await services.Storage.GetFile(resource.FileName, options);
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

        // Check file upload limits for users (not for shared files)
        if (userId != null)
        {
            var isCoach = services.IdentityService.IsCoach;
            var userType = isCoach ? UserType.Coach : UserType.User;

            // Only check limits for regular users, not coaches (coaches don't have file limits)
            if (userType == UserType.User)
            {
                var (allowed, errorMessage) = await services.FeatureGateService.CheckFeatureAccessAsync(userId, userType, "file_upload");
                if (!allowed)
                {
                    return Results.BadRequest(new { error = errorMessage ?? "File upload limit reached" });
                }
            }
        }

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

            // Check file size limit before uploading (for users only)
            if (userId != null)
            {
                var isCoach = services.IdentityService.IsCoach;

                if (!isCoach)
                {
                    var currentStorage = await services.UsageTrackingService.GetFileStorageUsageAsync(userId);
                    var tier = await services.SubscriptionService.GetUserTierAsync(userId, UserType.User);
                    var maxStorageMB = int.Parse(services.Config[$"TierLimits:User:{tier}:MaxFileStorageMB"] ?? "100");
                    var maxStorageBytes = maxStorageMB * 1024L * 1024L;

                    if (maxStorageMB >= 0 && (currentStorage + file.Length) > maxStorageBytes)
                    {
                        results.Add(new { status = "error", filename, message = $"Uploading this file would exceed your storage limit of {maxStorageMB}MB" });
                        continue;
                    }
                }
            }

            var resourceId = Guid.NewGuid();
            var fileStream = file.OpenReadStream();
            var options = new StorageUploadOptions
            {
                UserId = userId ?? string.Empty,
                FileOwnership = userId is null ? FileOwnership.Shared : FileOwnership.User,
                StorageType = StorageType.Resources,
                ResourceId = resourceId.ToString()
            };
            var location = await services.Storage.UploadFile(fileStream, filename, options);
            // var location = await services.Storage.UploadFile(fileStream, filename, resourceId.ToString(), userId);
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

            // Track file upload usage (for users only)
            if (userId != null)
            {
                await services.UsageTrackingService.TrackFileUploadAsync(userId, file.Length);
            }
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
        await services.Storage.DeleteFile(resource.FileName, new StorageOptions { UserId = resource.UserId, FileOwnership = resource.IsShared ? FileOwnership.Shared : FileOwnership.User, StorageType = StorageType.Resources });
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