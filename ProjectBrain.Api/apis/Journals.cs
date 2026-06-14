using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Background;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Shared.Dtos.Journal;
using ProjectBrain.Shared.Dtos.Pagination;
using ProjectBrain.Shared.Dtos.SystemTags;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;
using ProjectBrain.AI;

public class JournalServices(
    IJournalEntryService journalEntryService,
    ISystemTagService systemTagService,
    IUserProfileService userProfileService,
    IJournalStreakService journalStreakService,
    IIdentityService identityService,
    ILogger<JournalServices> logger,
    ITimeTickerManager<TimeTickerEntity> timeTickerManager,
    AzureOpenAI azureOpenAI)
{
    public ILogger<JournalServices> Logger { get; } = logger;
    public IJournalEntryService JournalEntryService { get; } = journalEntryService;
    public ISystemTagService SystemTagService { get; } = systemTagService;
    public IUserProfileService UserProfileService { get; } = userProfileService;
    public IJournalStreakService JournalStreakService { get; } = journalStreakService;
    public IIdentityService IdentityService { get; } = identityService;
    public ITimeTickerManager<TimeTickerEntity> TimeTickerManager { get; } = timeTickerManager;
    public AzureOpenAI AzureOpenAI { get; } = azureOpenAI;
}

public static class JournalEndpoints
{
    public static void MapJournalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("journal").RequireAuthorization();

        group.MapPost("/", CreateJournalEntry).WithName("CreateJournalEntry");
        group.MapGet("/{id:guid}", GetJournalEntryById).WithName("GetJournalEntryById");
        group.MapGet("/", GetAllJournalEntriesForUser).WithName("GetAllJournalEntriesForUser");
        group.MapGet("/count", GetJournalEntryCount).WithName("GetJournalEntryCount");
        group.MapGet("/recent", GetRecentJournalEntries).WithName("GetRecentJournalEntries");
        group.MapGet("/system-tags", GetSystemTags).WithName("GetSystemTags");
        group.MapGet("/streak-summary", GetJournalStreakSummary).WithName("GetJournalStreakSummary");
        group.MapPut("/{id:guid}", UpdateJournalEntry).WithName("UpdateJournalEntry");
        group.MapDelete("/{id:guid}", DeleteJournalEntry).WithName("DeleteJournalEntry");
    }

    private static async Task<IResult> CreateJournalEntry(
        [AsParameters] JournalServices services,
        CreateJournalEntryRequestDto request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var summary = await services.AzureOpenAI.GetConversationSummary(request.Content, userId);

        var journalEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = request.Content,
            Summary = summary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var systemAssignments = BuildSystemTagAssignments(request.SystemTagIds, request.SystemTagResponses);
        var createdEntry = await services.JournalEntryService.Add(journalEntry, request.TagIds, systemAssignments);

        // Enqueue: generate summary, upload blob, and index via TickerQ
        var entryId = createdEntry.Id;
        var entryUserId = userId;
        await UserContextTickerEnqueue.EnqueueJournalUploadAsync(services.TimeTickerManager, entryUserId, entryId);

        var dto = JournalEntryMapper.ToDto(createdEntry);
        return Results.Created($"/journal/{createdEntry.Id}", dto);
    }

    private static async Task<IResult> GetJournalEntryById(
        [AsParameters] JournalServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var journalEntry = await services.JournalEntryService.GetByIdWithTags(id, userId);
        if (journalEntry == null)
        {
            return Results.NotFound();
        }

        var dto = JournalEntryMapper.ToDto(journalEntry);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetAllJournalEntriesForUser(
        [AsParameters] JournalServices services,
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

        var totalCount = await services.JournalEntryService.CountForUser(userId);

        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var paginatedEntries = await services.JournalEntryService.GetPagedForUser(userId, skip, take);

        var entryDtos = JournalEntryMapper.ToDtoList(paginatedEntries);
        var response = PagedResponse<JournalEntryResponseDto>.Create(pagedRequest, entryDtos, totalCount);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetJournalEntryCount(
        [AsParameters] JournalServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var count = await services.JournalEntryService.CountForUser(userId);
        return Results.Ok(new JournalEntryCountResponseDto { Count = count });
    }

    private static async Task<IResult> GetRecentJournalEntries(
        [AsParameters] JournalServices services,
        int count = 3)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var recentEntries = await services.JournalEntryService.GetRecentForUser(userId, count);
        var entryDtos = JournalEntryMapper.ToDtoList(recentEntries);
        return Results.Ok(entryDtos);
    }

    private static async Task<IResult> UpdateJournalEntry(
        [AsParameters] JournalServices services,
        Guid id,
        UpdateJournalEntryRequestDto request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var journalEntry = await services.JournalEntryService.GetById(id, userId);
        if (journalEntry == null)
        {
            return Results.NotFound();
        }

        journalEntry.Content = request.Content;
        journalEntry.Summary = null; // Will be regenerated asynchronously
        journalEntry.UpdatedAt = DateTime.UtcNow;

        var systemAssignments = BuildSystemTagAssignments(request.SystemTagIds, request.SystemTagResponses);
        var updatedEntry = await services.JournalEntryService.Update(journalEntry, request.TagIds, systemAssignments);

        // Enqueue: generate summary, upload blob, and re-index via TickerQ
        var entryId = updatedEntry.Id;
        var entryUserId = userId;
        await UserContextTickerEnqueue.EnqueueJournalUploadAsync(services.TimeTickerManager, entryUserId, entryId);

        var dto = JournalEntryMapper.ToDto(updatedEntry);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetSystemTags(
        [AsParameters] JournalServices services)
    {
        var systemTags = await services.SystemTagService.GetAllWithFields();
        var dtos = SystemTagService.ToDtoList(systemTags);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetJournalStreakSummary(
        [AsParameters] JournalServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var userProfile = await services.UserProfileService.GetByUserId(userId);
        var timezoneId = TryGetTimezoneId(userProfile?.Preference?.Preferences);

        var dto = await services.JournalStreakService.GetStreakSummary(userId, timezoneId);
        return Results.Ok(dto);
    }

    private static string? TryGetTimezoneId(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("timezone", out var tzElement) &&
                tzElement.ValueKind == JsonValueKind.String)
            {
                return tzElement.GetString();
            }
        }
        catch
        {
            // Ignore invalid JSON; fall back to UTC
        }

        return null;
    }

    private static List<SystemTagAssignment>? BuildSystemTagAssignments(
        List<Guid>? systemTagIds,
        List<JournalEntrySystemTagRequestDto>? systemTagResponses)
    {
        var assignments = new List<SystemTagAssignment>();

        if (systemTagResponses != null)
        {
            foreach (var item in systemTagResponses)
            {
                if (item.SystemTagId == Guid.Empty)
                {
                    continue;
                }

                string? responsesJson = null;
                if (item.Responses != null && item.Responses.Count > 0)
                {
                    responsesJson = JsonSerializer.Serialize(item.Responses);
                }

                assignments.Add(new SystemTagAssignment(item.SystemTagId, responsesJson));
            }
        }

        if (systemTagIds != null)
        {
            foreach (var id in systemTagIds)
            {
                if (id == Guid.Empty)
                {
                    continue;
                }

                if (assignments.All(a => a.SystemTagId != id))
                {
                    assignments.Add(new SystemTagAssignment(id, null));
                }
            }
        }

        return assignments.Count > 0 ? assignments : null;
    }

    private static async Task<IResult> DeleteJournalEntry(
        [AsParameters] JournalServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var journalEntry = await services.JournalEntryService.GetById(id, userId);
        if (journalEntry == null)
        {
            return Results.NotFound();
        }

        await services.JournalEntryService.Remove(journalEntry);

        // Enqueue: delete from blob storage and search index via TickerQ
        var entryId = journalEntry.Id;
        var entryUserId = userId;
        await UserContextTickerEnqueue.EnqueueJournalDeleteAsync(services.TimeTickerManager, entryUserId, entryId);

        return Results.NoContent();
    }
}

