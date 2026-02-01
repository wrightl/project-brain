namespace ProjectBrain.Shared.Dtos.Habits;

public enum YearlyGoalsStatusDto
{
    NoneSet = 0,
    NoneCompleted = 1,
    SomeCompleted = 2,
    AllCompleted = 3,
}

public class YearlyHabitsCalendarDayDto
{
    /// <summary>
    /// Local date in user's timezone, formatted as yyyy-MM-dd.
    /// </summary>
    public required string Date { get; init; }

    public required bool HasJournalEntry { get; init; }

    public required YearlyGoalsStatusDto GoalsStatus { get; init; }
}

public class YearlyHabitsCalendarResponseDto
{
    /// <summary>
    /// Local start date in user's timezone, formatted as yyyy-MM-dd.
    /// </summary>
    public required string StartDate { get; init; }

    /// <summary>
    /// Local end date in user's timezone, formatted as yyyy-MM-dd.
    /// </summary>
    public required string EndDate { get; init; }

    public required IReadOnlyList<YearlyHabitsCalendarDayDto> Days { get; init; }
}

