namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Habits;

public class HabitsCalendarService : IHabitsCalendarService
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IGoalRepository _goalRepository;

    public HabitsCalendarService(
        IJournalEntryRepository journalEntryRepository,
        IGoalRepository goalRepository)
    {
        _journalEntryRepository = journalEntryRepository ?? throw new ArgumentNullException(nameof(journalEntryRepository));
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
    }

    public async Task<YearlyHabitsCalendarResponseDto> GetYearlyCalendar(
        string userId,
        string? timezoneId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        var tz = TryGetTimeZoneInfo(timezoneId) ?? TimeZoneInfo.Utc;

        var endDateLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
        var startDateLocal = endDateLocal.AddDays(-364);

        // UTC boundaries corresponding to local midnight boundaries
        var fromUtc = ToUtcAtStartOfDay(startDateLocal, tz);
        var toUtcExclusive = ToUtcAtStartOfDay(endDateLocal.AddDays(1), tz);

        // Journal: collect days that have at least one entry
        var createdAts = await _journalEntryRepository.GetCreatedAtForUserInRangeAsync(
            userId,
            fromUtc,
            toUtcExclusive,
            cancellationToken);

        var journalDaysLocal = createdAts
            .Select(dt => DateOnly.FromDateTime(ToLocalUtcAssumed(dt, tz)))
            .ToHashSet();

        // Goals: query a slightly wider UTC-date range to safely map DateOnly -> local date
        var fromGoalDateUtc = DateOnly.FromDateTime(fromUtc.AddDays(-1));
        var toGoalDateUtc = DateOnly.FromDateTime(toUtcExclusive.AddDays(1));
        var goals = await _goalRepository.GetGoalsInDateRangeAsync(
            userId,
            fromGoalDateUtc,
            toGoalDateUtc,
            cancellationToken);

        var goalsByLocalDate = goals
            .GroupBy(g => ToLocalDateFromGoalDateUtc(g.Date, tz))
            .ToDictionary(g => g.Key, g => g.ToList());

        var days = new List<YearlyHabitsCalendarDayDto>(capacity: 365);
        var cursor = startDateLocal;

        for (var i = 0; i < 365; i++)
        {
            var hasJournalEntry = journalDaysLocal.Contains(cursor);

            var goalsStatus = YearlyGoalsStatusDto.NoneSet;
            if (goalsByLocalDate.TryGetValue(cursor, out var dayGoals))
            {
                var activeGoals = dayGoals
                    .Where(g => !string.IsNullOrWhiteSpace(g.Message))
                    .ToList();

                if (activeGoals.Count > 0)
                {
                    var completedCount = activeGoals.Count(g => g.Completed);
                    if (completedCount == 0)
                    {
                        goalsStatus = YearlyGoalsStatusDto.NoneCompleted;
                    }
                    else if (completedCount == activeGoals.Count)
                    {
                        goalsStatus = YearlyGoalsStatusDto.AllCompleted;
                    }
                    else
                    {
                        goalsStatus = YearlyGoalsStatusDto.SomeCompleted;
                    }
                }
            }

            days.Add(new YearlyHabitsCalendarDayDto
            {
                Date = cursor.ToString("yyyy-MM-dd"),
                HasJournalEntry = hasJournalEntry,
                GoalsStatus = goalsStatus,
            });

            cursor = cursor.AddDays(1);
        }

        return new YearlyHabitsCalendarResponseDto
        {
            StartDate = startDateLocal.ToString("yyyy-MM-dd"),
            EndDate = endDateLocal.ToString("yyyy-MM-dd"),
            Days = days,
        };
    }

    private static DateTime ToLocalUtcAssumed(DateTime maybeUtc, TimeZoneInfo tz)
    {
        var utc = DateTime.SpecifyKind(maybeUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
    }

    private static DateOnly ToLocalDateFromGoalDateUtc(DateOnly goalDateUtc, TimeZoneInfo tz)
    {
        // Goal.Date is stored as DateOnly. We interpret it as midnight UTC for mapping into local day buckets.
        var utcMidnight = new DateTime(goalDateUtc.Year, goalDateUtc.Month, goalDateUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcMidnight, tz);
        return DateOnly.FromDateTime(local);
    }

    private static DateTime ToUtcAtStartOfDay(DateOnly localDate, TimeZoneInfo tz)
    {
        var localStart = localDate.ToDateTime(TimeOnly.MinValue);
        var unspecified = DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }

    private static TimeZoneInfo? TryGetTimeZoneInfo(string? timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch
        {
            return null;
        }
    }
}

public interface IHabitsCalendarService
{
    Task<YearlyHabitsCalendarResponseDto> GetYearlyCalendar(
        string userId,
        string? timezoneId,
        CancellationToken cancellationToken = default);
}

