namespace ProjectBrain.Shared.Dtos.Journal;

public class JournalStreakSummaryResponseDto
{
    public required int CurrentStreak { get; init; }
    public required int LongestStreak { get; init; }
}

