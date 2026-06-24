namespace ProjectBrain.Domain;

using System.Security.Cryptography;
using System.Text;

public static class MemoryHashHelper
{
    public static string ComputeHash(string content)
    {
        var normalized = content.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
