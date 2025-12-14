namespace ProjectBrain.Domain.Constants;

/// <summary>
/// Constants for connection status values
/// </summary>
public static class ConnectionStatus
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Cancelled = "cancelled";
    public const string Rejected = "rejected";

    /// <summary>
    /// Validates if a status is valid
    /// </summary>
    public static bool IsValid(string status)
    {
        return status == Pending || 
               status == Accepted || 
               status == Cancelled || 
               status == Rejected;
    }
}

