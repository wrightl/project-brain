using System.Text;
using ProjectBrain.AI;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Exceptions;

namespace ProjectBrain.Api.Services;

public sealed class UserKnowledgeUploadService : IUserKnowledgeUploadService
{
    private readonly IResourceService _resourceService;
    private readonly Storage _storage;
    private readonly IFeatureGateService _featureGateService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IConfiguration _configuration;

    public UserKnowledgeUploadService(
        IResourceService resourceService,
        Storage storage,
        IFeatureGateService featureGateService,
        ISubscriptionService subscriptionService,
        IUsageTrackingService usageTrackingService,
        IConfiguration configuration)
    {
        _resourceService = resourceService;
        _storage = storage;
        _featureGateService = featureGateService;
        _subscriptionService = subscriptionService;
        _usageTrackingService = usageTrackingService;
        _configuration = configuration;
    }

    public async Task<KnowledgeUploadResult> UploadMarkdownAsync(
        string userId,
        string filename,
        string markdown,
        CancellationToken cancellationToken = default)
    {
        var (allowed, errorMessage) = await _featureGateService.CheckFeatureAccessAsync(userId, UserType.User, "file_upload");
        if (!allowed)
        {
            return new KnowledgeUploadResult { Success = false, Message = errorMessage ?? "File upload limit reached" };
        }

        filename = FileUploadSecurity.SanitizeFileName(filename);
        if (!filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            filename += ".md";
        }

        var bytes = Encoding.UTF8.GetBytes(markdown);
        FileUploadSecurity.ValidateUpload(filename, bytes.Length, "text/markdown");

        var existing = await _resourceService.GetForUserByFilename(filename, userId);
        if (existing is not null)
        {
            return new KnowledgeUploadResult { Success = false, Filename = filename, Message = "File already exists" };
        }

        var currentStorage = await _usageTrackingService.GetFileStorageUsageAsync(userId);
        var tier = await _subscriptionService.GetUserTierAsync(userId, UserType.User);
        var maxStorageMB = int.Parse(_configuration[$"TierLimits:User:{tier}:MaxFileStorageMB"] ?? "100");
        var maxStorageBytes = maxStorageMB * 1024L * 1024L;

        if (maxStorageMB >= 0 && (currentStorage + bytes.Length) > maxStorageBytes)
        {
            return new KnowledgeUploadResult
            {
                Success = false,
                Filename = filename,
                Message = $"Uploading this file would exceed your storage limit of {maxStorageMB}MB"
            };
        }

        var resourceId = Guid.NewGuid();
        using var stream = new MemoryStream(bytes);
        var options = new StorageUploadOptions
        {
            UserId = userId,
            FileOwnership = FileOwnership.User,
            StorageType = StorageType.Resources,
            ResourceId = resourceId.ToString()
        };

        var location = await _storage.UploadFile(stream, filename, options);

        await _resourceService.Add(new Resource
        {
            Id = resourceId,
            FileName = filename,
            Location = location,
            SizeInBytes = bytes.Length,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsShared = false
        });

        await _usageTrackingService.TrackFileUploadAsync(userId, bytes.Length);

        return new KnowledgeUploadResult
        {
            Success = true,
            ResourceId = resourceId,
            Filename = filename,
            Message = "Knowledge document uploaded and indexed"
        };
    }

    public async Task<IReadOnlyList<KnowledgeResourceSummary>> ListResourcesAsync(
        string userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var resources = await _resourceService.GetAllForUser(userId, limit);
        return resources.Select(r => new KnowledgeResourceSummary
        {
            Id = r.Id,
            FileName = r.FileName,
            SizeInBytes = r.SizeInBytes,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<bool> DeleteResourceAsync(
        string userId,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _resourceService.GetForUserById(resourceId, userId);
        if (resource is null)
        {
            return false;
        }

        await _storage.DeleteFile(resource.FileName, new StorageOptions
        {
            UserId = userId,
            FileOwnership = FileOwnership.User,
            StorageType = StorageType.Resources
        });
        await _resourceService.Remove(resource);
        return true;
    }
}
