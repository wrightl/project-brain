using System.Text.RegularExpressions;
using ProjectBrain.Domain.Exceptions;

namespace ProjectBrain.Api;

public static class FileUploadSecurity
{
    private const long DefaultMaxFileSizeBytes = 50 * 1024 * 1024;

    private static readonly string[] DefaultAllowedExtensions =
    [
        ".pdf", ".doc", ".docx", ".txt", ".md", ".json", ".html", ".htm",
        ".xlsx", ".xls", ".pptx", ".ppt", ".png", ".jpg", ".jpeg", ".csv"
    ];

    public static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("filename", "Filename is required");
        }

        var sanitized = Path.GetFileName(fileName.Trim());
        sanitized = sanitized.Replace('\\', '/');
        sanitized = sanitized.Split('/').Last();

        if (string.IsNullOrWhiteSpace(sanitized) ||
            sanitized is "." or ".." ||
            sanitized.Contains("..", StringComparison.Ordinal))
        {
            throw new ValidationException("filename", "Invalid filename");
        }

        if (!Regex.IsMatch(sanitized, @"^[a-zA-Z0-9._ -]+$"))
        {
            throw new ValidationException("filename", "Filename contains invalid characters");
        }

        return sanitized;
    }

    public static void ValidateUpload(
        string fileName,
        long fileSize,
        string? contentType = null,
        long maxFileSizeBytes = DefaultMaxFileSizeBytes,
        string[]? allowedExtensions = null)
    {
        if (fileSize <= 0)
        {
            throw new ValidationException("file", "File is empty");
        }

        if (fileSize > maxFileSizeBytes)
        {
            throw new ValidationException("file", $"File exceeds maximum size of {maxFileSizeBytes / (1024 * 1024)}MB");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var extensions = allowedExtensions ?? DefaultAllowedExtensions;

        if (string.IsNullOrWhiteSpace(extension) || !extensions.Contains(extension))
        {
            throw new ValidationException("file", $"File type '{extension}' is not allowed");
        }
    }
}
