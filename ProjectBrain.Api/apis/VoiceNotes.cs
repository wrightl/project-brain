using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class VoiceNoteServices(
    ILogger<VoiceNoteServices> logger,
    IVoiceNoteService voiceNoteService,
    Storage storage,
    IIdentityService identityService,
    IConfiguration configuration)
{
    public ILogger<VoiceNoteServices> Logger { get; } = logger;
    public IVoiceNoteService VoiceNoteService { get; } = voiceNoteService;
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
            return Results.Unauthorized();
        }

        try
        {
            // Parse limit query parameter
            int? limit = null;
            if (request.Query.TryGetValue("limit", out var limitValue) &&
                int.TryParse(limitValue, out var parsedLimit) &&
                parsedLimit > 0)
            {
                limit = parsedLimit;
            }

            var voiceNotes = await services.VoiceNoteService.GetAllForUser(userId, limit);
            var response = voiceNotes.Select(vn => new
            {
                id = vn.Id.ToString(),
                fileName = vn.FileName,
                audioUrl = vn.AudioUrl,
                duration = vn.Duration,
                fileSize = vn.FileSize,
                description = vn.Description,
                createdAt = vn.CreatedAt.ToString("O"),
                updatedAt = vn.UpdatedAt.ToString("O")
            });

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving voice notes for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving voice notes.");
        }
    }

    private static async Task<IResult> UploadVoiceNote([AsParameters] VoiceNoteServices services, HttpRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var form = await request.ReadFormAsync();

            if (form.Files.Count == 0)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "MISSING_FILE",
                        message = "No file provided in upload request"
                    }
                });
            }

            var file = form.Files[0];
            var description = form["description"].ToString();
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "MISSING_FILE",
                        message = "File is empty"
                    }
                });
            }

            // Validate file size
            if (file.Length > MaxFileSize)
            {
                return Results.Problem(
                    detail: "File size exceeds maximum limit of 50MB",
                    statusCode: 413,
                    title: "Payload Too Large",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = new
                        {
                            code = "FILE_TOO_LARGE",
                            message = $"File size exceeds maximum limit of 50MB",
                            details = new
                            {
                                maxSize = MaxFileSize,
                                providedSize = file.Length
                            }
                        }
                    });
            }

            // Validate file type
            var contentType = file.ContentType;
            var fileName = file.FileName ?? "voice_note.m4a";
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension) && !AllowedMimeTypes.Contains(contentType))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_FILE_FORMAT",
                        message = $"File format not supported. Allowed formats: {string.Join(", ", AllowedExtensions)}",
                        details = new
                        {
                            providedMimeType = contentType,
                            providedExtension = extension
                        }
                    }
                });
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

            var response = new
            {
                id = savedVoiceNote.Id.ToString(),
                fileName = savedVoiceNote.FileName,
                audioUrl = savedVoiceNote.AudioUrl,
                duration = savedVoiceNote.Duration,
                fileSize = savedVoiceNote.FileSize,
                description = savedVoiceNote.Description,
                createdAt = savedVoiceNote.CreatedAt.ToString("O"),
                updatedAt = savedVoiceNote.UpdatedAt.ToString("O")
            };

            return Results.Created($"/voicenotes/{savedVoiceNote.Id}", response);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error uploading voice note for user {UserId}", userId);
            return Results.Problem(
                detail: "An error occurred while uploading the voice note",
                statusCode: 500,
                title: "Upload Failed",
                extensions: new Dictionary<string, object?>
                {
                    ["error"] = new
                    {
                        code = "UPLOAD_FAILED",
                        message = "Failed to upload voice note"
                    }
                });
        }
    }

    private static async Task<IResult> GetVoiceNoteAudio(
        [AsParameters] VoiceNoteServices services,
        string voiceNoteId,
        HttpRequest request)
    {
        var userId = services.IdentityService.UserId!;

        Console.WriteLine($"Getting voice note audio for user {userId} and voice note {voiceNoteId}");
        try
        {
            if (!Guid.TryParse(voiceNoteId, out var id))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_ID",
                        message = "Invalid voice note ID format"
                    }
                });
            }

            var voiceNote = await services.VoiceNoteService.GetById(id, userId);
            if (voiceNote == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "VOICE_NOTE_NOT_FOUND",
                        message = "The specified voice note does not exist"
                    }
                });
            }

            // Check if user owns this voice note (already checked in GetById, but double-check)
            if (voiceNote.UserId != userId)
            {
                return Results.Forbid();
            }

            // Get file stream from storage
            var fileStream = await services.Storage.GetFile(voiceNote.FilePath);
            if (fileStream == null)
            {
                return Results.NotFound(new
                {
                    error = new
                    {
                        code = "FILE_NOT_FOUND",
                        message = "Audio file not found in storage"
                    }
                });
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
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving voice note audio {VoiceNoteId} for user {UserId}", voiceNoteId, userId);
            return Results.Problem("An error occurred while retrieving the audio file.");
        }
    }

    private static async Task<IResult> DeleteVoiceNote(
        [AsParameters] VoiceNoteServices services,
        string voiceNoteId)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            if (!Guid.TryParse(voiceNoteId, out var id))
            {
                return Results.BadRequest(new
                {
                    error = new
                    {
                        code = "INVALID_ID",
                        message = "Invalid voice note ID format"
                    }
                });
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
                return Results.Forbid();
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
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error deleting voice note {VoiceNoteId} for user {UserId}", voiceNoteId, userId);
            return Results.Problem("An error occurred while deleting the voice note.");
        }
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

