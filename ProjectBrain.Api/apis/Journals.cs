using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.AI;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Journal;
using ProjectBrain.Shared.Dtos.Pagination;

public class JournalServices(
    IJournalEntryService journalEntryService,
    IJournalEntryRepository journalEntryRepository,
    IIdentityService identityService,
    ILogger<JournalServices> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    public ILogger<JournalServices> Logger { get; } = logger;
    public IJournalEntryService JournalEntryService { get; } = journalEntryService;
    public IJournalEntryRepository JournalEntryRepository { get; } = journalEntryRepository;
    public IIdentityService IdentityService { get; } = identityService;
    public IServiceScopeFactory ServiceScopeFactory { get; } = serviceScopeFactory;
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

        var journalEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Content = request.Content,
            Summary = null, // Will be generated asynchronously
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdEntry = await services.JournalEntryService.Add(journalEntry, request.TagIds);

        // Fire-and-forget: Generate summary, upload blob, and index asynchronously
        var entryId = createdEntry.Id;
        var entryContent = createdEntry.Content;
        var entryUserId = userId;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = services.ServiceScopeFactory.CreateScope();
                var azureOpenAI = scope.ServiceProvider.GetRequiredService<AzureOpenAI>();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                var searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();
                var journalEntryService = scope.ServiceProvider.GetRequiredService<IJournalEntryService>();

                // Generate summary using Azure OpenAI
                var summary = await azureOpenAI.GetConversationSummary(entryContent, entryUserId);

                // Update the entry with the summary
                var entry = await journalEntryService.GetById(entryId, entryUserId);
                if (entry != null)
                {
                    entry.Summary = summary;
                    entry.UpdatedAt = DateTime.UtcNow;
                    await journalEntryService.Update(entry, null);
                }

                // Upload to blob storage as JSON
                var blobPath = $"journal/{entryUserId}/{entryId}.json";
                var jsonContent = JsonSerializer.Serialize(new
                {
                    id = entryId.ToString(),
                    userId = entryUserId,
                    content = entryContent,
                    summary = summary,
                    createdAt = entry?.CreatedAt ?? DateTime.UtcNow,
                    updatedAt = DateTime.UtcNow
                }, new JsonSerializerOptions { WriteIndented = false });

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
                await storage.UploadFile(stream, $"{entryId}.json", entryId.ToString(), entryUserId, skipIndexing: true, parentFolder: "journal");

                // Index in Azure Search
                using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(entryContent));
                await searchIndexService.ExtractEmbedAndIndexFromStreamAsync(
                    contentStream,
                    $"{entryId}.json",
                    entryUserId,
                    blobPath,
                    entryId.ToString());

                services.Logger.LogInformation("Successfully processed journal entry {EntryId} asynchronously", entryId);
            }
            catch (Exception ex)
            {
                services.Logger.LogError(ex, "Error processing journal entry {EntryId} asynchronously", entryId);
            }
        });

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

        // Get total count for pagination
        var totalCount = await services.JournalEntryRepository.CountForUserAsync(userId, CancellationToken.None);

        // Get paginated results
        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var paginatedEntries = await services.JournalEntryRepository.GetPagedForUserAsync(userId, skip, take, CancellationToken.None);

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

        var updatedEntry = await services.JournalEntryService.Update(journalEntry, request.TagIds);

        // Fire-and-forget: Generate summary, upload blob, and re-index asynchronously
        var entryId = updatedEntry.Id;
        var entryContent = updatedEntry.Content;
        var entryUserId = userId;
        var entryCreatedAt = updatedEntry.CreatedAt;

        // TODO: Do this properly with queues and background services
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = services.ServiceScopeFactory.CreateScope();
                var azureOpenAI = scope.ServiceProvider.GetRequiredService<AzureOpenAI>();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                var searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();
                var journalEntryService = scope.ServiceProvider.GetRequiredService<IJournalEntryService>();

                // Generate new summary
                var summary = await azureOpenAI.GetConversationSummary(entryContent, entryUserId);

                // Update the entry with the summary
                var entry = await journalEntryService.GetById(entryId, entryUserId);
                if (entry != null)
                {
                    entry.Summary = summary;
                    entry.UpdatedAt = DateTime.UtcNow;
                    await journalEntryService.Update(entry, null);
                }

                // Update blob storage
                var blobPath = $"journal/{entryUserId}/{entryId}.json";
                var jsonContent = JsonSerializer.Serialize(new
                {
                    id = entryId.ToString(),
                    userId = entryUserId,
                    content = entryContent,
                    summary = summary,
                    createdAt = entryCreatedAt,
                    updatedAt = DateTime.UtcNow
                }, new JsonSerializerOptions { WriteIndented = false });

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
                await storage.UploadFile(stream, $"{entryId}.json", entryId.ToString(), entryUserId, skipIndexing: true, parentFolder: "journal");

                // Re-index in Azure Search
                using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(entryContent));
                await searchIndexService.ExtractEmbedAndIndexFromStreamAsync(
                    contentStream,
                    $"{entryId}.json",
                    entryUserId,
                    blobPath,
                    entryId.ToString());

                services.Logger.LogInformation("Successfully processed journal entry update {EntryId} asynchronously", entryId);
            }
            catch (Exception ex)
            {
                services.Logger.LogError(ex, "Error processing journal entry update {EntryId} asynchronously", entryId);
            }
        });

        var dto = JournalEntryMapper.ToDto(updatedEntry);
        return Results.Ok(dto);
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

        // Fire-and-forget: Delete from blob storage and search index asynchronously
        var entryId = journalEntry.Id;
        var entryUserId = userId;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = services.ServiceScopeFactory.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();

                // Delete from blob storage
                var blobPath = $"journal/{entryUserId}/{entryId}.json";
                await storage.DeleteFile(blobPath);

                // Delete from search index (would need to implement this in search service)
                // For now, we'll leave the search index cleanup to a background job or manual process

                services.Logger.LogInformation("Successfully deleted journal entry {EntryId} blob asynchronously", entryId);
            }
            catch (Exception ex)
            {
                services.Logger.LogError(ex, "Error deleting journal entry {EntryId} blob asynchronously", entryId);
            }
        });

        return Results.NoContent();
    }
}

