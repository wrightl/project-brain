namespace ProjectBrain.AI;

using System.Text;
using ProjectBrain.Domain.Dtos;
using _shared = Models;

/// <summary>Builds stable and volatile sections of chat prompts from memory context.</summary>
public static class ChatPromptAssembler
{
    public static IReadOnlyList<_shared.ChatMessage> SelectRecentHistory(
        IReadOnlyList<_shared.ChatMessage> history,
        ChatMemoryContext memoryContext)
    {
        if (history.Count == 0)
        {
            return history;
        }

        var hasSummary = memoryContext.EnableConversationSummary
            && !string.IsNullOrWhiteSpace(memoryContext.ConversationSummary);

        var window = hasSummary
            ? memoryContext.RecentMessageWindow
            : memoryContext.MaxHistoryMessages;

        return history.TakeLast(Math.Max(1, window)).ToList();
    }

    public static string FormatPoliciesBlock(IReadOnlyList<ChatPolicyItem> policies)
    {
        if (policies.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("## Policies");
        foreach (var policy in policies)
        {
            var label = policy.Key.StartsWith("AI:Policy:", StringComparison.Ordinal)
                ? policy.Key["AI:Policy:".Length..]
                : policy.Key;
            sb.AppendLine($"- {label}: {policy.Value}");
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatPreferencesBlock(UserChatPreferences preferences)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## User preferences");

        if (!string.IsNullOrWhiteSpace(preferences.PreferredPronoun))
        {
            sb.AppendLine($"- Preferred pronoun: {preferences.PreferredPronoun}");
        }

        if (preferences.NeurodiverseTraits.Count > 0)
        {
            sb.AppendLine($"- Neurodiverse traits: {string.Join(", ", preferences.NeurodiverseTraits)}");
        }

        foreach (var kvp in preferences.ParsedPreferences)
        {
            sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
        }

        if (!string.IsNullOrWhiteSpace(preferences.PreferencesJson)
            && preferences.ParsedPreferences.Count == 0)
        {
            sb.AppendLine($"- Raw preferences JSON: {preferences.PreferencesJson}");
        }

        return sb.ToString().TrimEnd();
    }

    public static string FormatSummaryBlock(string summary)
    {
        return $"## Conversation so far\n{summary.Trim()}";
    }

    public static string BuildSystemPrompt(
        string userName,
        bool hasSources,
        ChatMemoryContext memoryContext)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are a friendly and supportive assistant helping neurodiverse individuals.");
        prompt.AppendLine("Provide helpful, clear, and empathetic responses that are easy to understand.");
        prompt.AppendLine();

        var policiesBlock = FormatPoliciesBlock(memoryContext.Policies);
        if (!string.IsNullOrWhiteSpace(policiesBlock))
        {
            prompt.AppendLine(policiesBlock);
            prompt.AppendLine();
        }

        if (memoryContext.UserPreferences is { } prefs)
        {
            prompt.AppendLine(FormatPreferencesBlock(prefs));
            prompt.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            prompt.AppendLine($"You are chatting with {userName}.");
            prompt.AppendLine();
        }

        if (hasSources)
        {
            prompt.AppendLine("- Base your response on the provided sources, the user's query and the conversation history");
            prompt.AppendLine("- Ignore any sources that are not relevant to the user's query or conversation history");
        }
        else
        {
            prompt.AppendLine("- No specific sources were found, but provide helpful general guidance");
        }

        prompt.AppendLine();
        prompt.AppendLine("Response structure:");
        prompt.AppendLine("- Answer the user's query clearly and thoroughly");
        prompt.AppendLine("- At the end, suggest 2-3 relevant follow-up questions that might help");
        prompt.AppendLine("- If clarification is needed, ask naturally within your response");

        return prompt.ToString();
    }

    public static string BuildUserPrompt(
        string userQuery,
        string userInformation,
        string sources,
        int citationCount,
        ChatMemoryContext memoryContext)
    {
        var prompt = new StringBuilder();

        if (memoryContext.EnableConversationSummary
            && !string.IsNullOrWhiteSpace(memoryContext.ConversationSummary))
        {
            prompt.AppendLine(FormatSummaryBlock(memoryContext.ConversationSummary));
            prompt.AppendLine();
        }

        if (citationCount > 0)
        {
            prompt.AppendLine($"Here are {citationCount} relevant sources from the user's documents:");
            prompt.AppendLine("---");
            prompt.AppendLine(sources);
            prompt.AppendLine("---");
            prompt.AppendLine("Answer the query using the sources above. Cite sources with [number] format. If the sources don't help answer the question, ignore them completely.");
            prompt.AppendLine();
        }

        prompt.AppendLine("---");
        prompt.AppendLine("Here is some data in json format about the user based on their onboarding data:");
        prompt.AppendLine(userInformation);
        prompt.AppendLine("---");

        prompt.AppendLine("User Query:");
        prompt.AppendLine(userQuery);

        return prompt.ToString();
    }

    public static string BuildStrategySystemPrompt(string userName, ChatMemoryContext memoryContext)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("You are a friendly and supportive assistant helping neurodiverse individuals.");
        prompt.AppendLine("Your job is to suggest practical coping strategies that are safe, gentle, and actionable.");

        var policiesBlock = FormatPoliciesBlock(memoryContext.Policies);
        if (!string.IsNullOrWhiteSpace(policiesBlock))
        {
            prompt.AppendLine();
            prompt.AppendLine(policiesBlock);
        }

        if (memoryContext.UserPreferences is { } prefs)
        {
            prompt.AppendLine();
            prompt.AppendLine(FormatPreferencesBlock(prefs));
        }

        prompt.AppendLine();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            prompt.AppendLine($"You are chatting with {userName}. Use their name occasionally and naturally.");
            prompt.AppendLine();
        }

        prompt.AppendLine("Return ONLY valid JSON. No markdown. No extra text.");
        prompt.AppendLine("Return exactly 3 items in this shape:");
        prompt.AppendLine("{\"items\":[{\"title\":\"...\",\"description\":\"...\",\"iconKey\":\"sparkles|lightbulb|null\",\"articleUrl\":\"https://...|null\"}]}");
        prompt.AppendLine("Constraints:");
        prompt.AppendLine("- titles <= 60 chars");
        prompt.AppendLine("- descriptions <= 280 chars");
        prompt.AppendLine("- descriptions must be specific steps the user can try");
        prompt.AppendLine("- articleUrl must be https and from one of these domains (or null): nhs.uk, apa.org, mind.org.uk, helpguide.org");

        return prompt.ToString();
    }
}
