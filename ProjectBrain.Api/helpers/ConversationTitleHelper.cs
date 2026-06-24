public static class ConversationTitleHelper
{
    public static string BuildPlaceholderTitle(string content)
    {
        var trimmed = content.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return "New chat";
        }

        const int maxLen = 128;
        if (trimmed.Length <= maxLen)
        {
            return trimmed;
        }

        return trimmed[..(maxLen - 3)] + "...";
    }
}
