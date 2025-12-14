using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Mappers;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Pagination;
using ProjectBrain.Shared.Dtos.VoiceNotes;

public class VoiceNoteServices(
    ILogger<VoiceNoteServices> logger,
    IVoiceNoteService voiceNoteService,
    IVoiceNoteRepository voiceNoteRepository,
    Storage storage,
    IIdentityService identityService,
    IConfiguration configuration)
{
    public ILogger<VoiceNoteServices> Logger { get; } = logger;
    public IVoiceNoteService VoiceNoteService { get; } = voiceNoteService;
    public IVoiceNoteRepository VoiceNoteRepository { get; } = voiceNoteRepository;
    public Storage Storage { get; } = storage;
    public IIdentityService IdentityService { get; } = identityService;
    public IConfiguration Configuration { get; } = configuration;
}

public static class VoiceNoteEndpoints
{
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    private static readonly string[] AllowedMimeTypes = { "audio/m4a", "audio/mpeg", "audio/aac", "audio/wav", "audio/x-m4a", "audio/mp4" };
    private static readonly string[] AllowedExtensions = { ".m4a", ".mp3", ".aac", ".wav" };

    public static void MapVoiceNoteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("voicenotes").RequireAuthorization();

        group.MapGet("/", GetAllVoiceNotes).WithName("GetAllVoiceNotes");
        group.MapPost("/", UploadVoiceNote).WithName("UploadVoiceNote");
        group.MapGet("/{voiceNoteId}/audio", GetVoiceNoteAudio).WithName("GetVoiceNoteAudio");
        group.MapDelete("/{voiceNoteId}", DeleteVoiceNote).WithName("DeleteVoiceNote");
    }

    private static async Task<IResult> GetAllVoiceNotes(
        [AsParameters] VoiceNoteServices services,
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
        var totalCount = await services.VoiceNoteRepository.CountAsync(
            vn => vn.UserId == userId,
            CancellationToken.None);

        // Get paginated results using efficient database-level pagination
        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var paginatedNotes = await services.VoiceNoteRepository.GetPagedForUserAsync(userId, skip, take, CancellationToken.None);

        var voiceNoteDtos = VoiceNoteMapper.ToDto(paginatedNotes);
        var response = PagedResponse<VoiceNoteResponseDto>.Create(pagedRequest, voiceNoteDtos, totalCount);

        return Results.Ok(response);
    }

    private static async Task<IResult> UploadVoiceNote([AsParameters] VoiceNoteServices services, HttpRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        var form = await request.ReadFormAsync();

        if (form.Files.Count == 0)
        {
            throw new ValidationException("file", "No file provided in upload request");
        }

        var file = form.Files[0];
        var description = form["description"].ToString();
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("file", "File is empty");
        }

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            throw new ValidationException("file", $"File size exceeds maximum limit of 50MB. Provided: {file.Length} bytes, Maximum: {MaxFileSize} bytes");
        }

        // Validate file type
        var contentType = file.ContentType;
        var fileName = file.FileName ?? "voice_note.m4a";
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension) && !AllowedMimeTypes.Contains(contentType))
        {
            throw new ValidationException("file", $"File format not supported. Allowed formats: {string.Join(", ", AllowedExtensions)}");
        }

        // Generate unique filename
        var voiceNoteId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sanitizedFileName = SanitizeFileName(fileName);
        var uniqueFileName = $"{voiceNoteId}_{timestamp}{extension}";

        // Upload file to storage
        // Use voicenotes/{userId}/{voiceNoteId}.{ext} format
        var storageFileName = $"{voiceNoteId}{extension}";
        var location = await UploadAudioFile(services, file, userId, storageFileName);

        // Build audio URL
        var baseUrl = services.Configuration["ApiBaseUrl"] ??
                     $"{request.Scheme}://{request.Host}";
        var audioUrl = $"{baseUrl}/voicenotes/{voiceNoteId}/audio";

        // Create voice note record
        var voiceNote = new VoiceNote
        {
            Id = voiceNoteId,
            UserId = userId,
            FileName = sanitizedFileName,
            FilePath = location,
            AudioUrl = audioUrl,
            FileSize = file.Length,
            MimeType = contentType ?? GetMimeTypeFromExtension(extension),
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // TODO: Extract duration from audio file if possible
        // For now, duration will be null

        var savedVoiceNote = await services.VoiceNoteService.Add(voiceNote);
        var response = VoiceNoteMapper.ToDto(savedVoiceNote);

        return Results.Created($"/voicenotes/{savedVoiceNote.Id}", response);
    }

    private static async Task<IResult> GetVoiceNoteAudio(
        [AsParameters] VoiceNoteServices services,
        string voiceNoteId,
        HttpRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(voiceNoteId, out var id))
        {
            throw new ValidationException("voiceNoteId", "Invalid voice note ID format");
        }

        var voiceNote = await services.VoiceNoteService.GetById(id, userId);
        if (voiceNote == null)
        {
            throw new NotFoundException("Voice note", id);
        }

        // Check if user owns this voice note (already checked in GetById, but double-check)
        if (voiceNote.UserId != userId)
        {
            throw new AppException("FORBIDDEN", "You do not have access to this voice note", 403);
        }

        // Get file stream from storage
        var fileStream = await services.Storage.GetFile(voiceNote.FilePath);
        if (fileStream == null)
        {
            throw new NotFoundException("Audio file");
        }

        // Support HTTP Range requests for audio streaming
        var rangeHeader = request.Headers.Range.ToString();
        if (!string.IsNullOrEmpty(rangeHeader) && fileStream.CanSeek)
        {
            // Parse range header (simplified - handles "bytes=start-end" format)
            var rangeResult = ParseRangeHeader(rangeHeader, voiceNote.FileSize ?? 0);
            if (rangeResult.HasValue)
            {
                fileStream.Position = rangeResult.Value.Start;
                var length = rangeResult.Value.End - rangeResult.Value.Start + 1;

                return Results.File(
                    fileStream,
                    contentType: voiceNote.MimeType,
                    fileDownloadName: voiceNote.FileName,
                    enableRangeProcessing: true,
                    lastModified: voiceNote.UpdatedAt);
            }
        }

        return Results.File(
            fileStream,
            contentType: voiceNote.MimeType,
            fileDownloadName: voiceNote.FileName,
            enableRangeProcessing: true,
            lastModified: voiceNote.UpdatedAt);
    }

    private static async Task<IResult> DeleteVoiceNote(
        [AsParameters] VoiceNoteServices services,
        string voiceNoteId)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        if (!Guid.TryParse(voiceNoteId, out var id))
        {
            throw new ValidationException("voiceNoteId", "Invalid voice note ID format");
        }

        var voiceNote = await services.VoiceNoteService.GetById(id, userId);
        if (voiceNote == null)
        {
            // Return success for idempotency (already deleted or doesn't exist)
            return Results.NoContent();
        }

        // Check ownership
        if (voiceNote.UserId != userId)
        {
            throw new AppException("FORBIDDEN", "You do not have access to this voice note", 403);
        }

        // Delete file from storage
        try
        {
            await services.Storage.DeleteFile(voiceNote.FilePath);
        }
        catch (Exception ex)
        {
            services.Logger.LogWarning(ex, "Failed to delete file from storage for voice note {VoiceNoteId}, continuing with database deletion", id);
            // Continue with database deletion even if file deletion fails
        }

        // Delete from database
        await services.VoiceNoteService.Delete(id, userId);

        return Results.NoContent();
    }

    private static async Task<string> UploadAudioFile(
        VoiceNoteServices services,
        IFormFile file,
        string userId,
        string fileName)
    {
        // Use a simpler upload path for voice notes: voicenotes/{userId}/{fileName}
        var blobPath = $"voicenotes/{userId}/{fileName}";

        var metadata = new Dictionary<string, string>
        {
            { "userId", userId },
            { "mimeType", file.ContentType ?? "audio/m4a" }
        };

        return await services.Storage.UploadAudioFile(file, blobPath, userId, metadata);
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "voice_note.m4a";

        // Remove path separators and other dangerous characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            sanitized = sanitized[..(200 - extension.Length)] + extension;
        }

        return sanitized;
    }

    private static string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".m4a" => "audio/m4a",
            ".mp3" => "audio/mpeg",
            ".aac" => "audio/aac",
            ".wav" => "audio/wav",
            _ => "audio/m4a"
        };
    }

    private static (long Start, long End)? ParseRangeHeader(string rangeHeader, long fileSize)
    {
        // Simple range parser - handles "bytes=start-end" format
        if (!rangeHeader.StartsWith("bytes="))
            return null;

        var range = rangeHeader["bytes=".Length..];
        var parts = range.Split('-');
        if (parts.Length != 2)
            return null;

        if (long.TryParse(parts[0], out var start) && long.TryParse(parts[1], out var end))
        {
            if (start < 0 || end >= fileSize || start > end)
                return null;

            return (start, end);
        }

        // Handle "bytes=start-" (from start to end of file)
        if (long.TryParse(parts[0], out start) && string.IsNullOrEmpty(parts[1]))
        {
            if (start < 0 || start >= fileSize)
                return null;

            return (start, fileSize - 1);
        }

        return null;
    }
}

