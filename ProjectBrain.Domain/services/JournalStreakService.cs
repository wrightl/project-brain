namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Journal;

public class JournalStreakService : IJournalStreakService
{
    private readonly IJournalEntryRepository _journalEntryRepository;

    public JournalStreakService(IJournalEntryRepository journalEntryRepository)
    {
        _journalEntryRepository = journalEntryRepository ?? throw new ArgumentNullException(nameof(journalEntryRepository));
    }

    public async Task<JournalStreakSummaryResponseDto> GetStreakSummary(string userId, string? timezoneId, CancellationToken cancellationToken = default)
    {
        var tz = TryGetTimeZoneInfo(timezoneId) ?? TimeZoneInfo.Utc;

        var createdAts = await _journalEntryRepository.GetCreatedAtForUserAsync(userId, cancellationToken);

        var daysWithEntries = createdAts
            .Select(dt => DateOnly.FromDateTime(ToLocalUtcAssumed(dt, tz)))
            .Distinct()
            .ToHashSet();

        if (daysWithEntries.Count == 0)
        {
            return new JournalStreakSummaryResponseDto { CurrentStreak = 0, LongestStreak = 0 };
        }

        var todayLocal = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));

        var currentStreak = 0;
        var cursor = todayLocal;
        while (daysWithEntries.Contains(cursor))
        {
            currentStreak++;
            cursor = cursor.AddDays(-1);
        }

        var longestStreak = 0;
        var ordered = daysWithEntries.OrderBy(d => d).ToList();
        var run = 1;
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].DayNumber == ordered[i - 1].DayNumber + 1)
            {
                run++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, run);
                run = 1;
            }
        }
        longestStreak = Math.Max(longestStreak, run);

        return new JournalStreakSummaryResponseDto
        {
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak
        };
    }

    private static DateTime ToLocalUtcAssumed(DateTime maybeUtc, TimeZoneInfo tz)
    {
        var utc = DateTime.SpecifyKind(maybeUtc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
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

public interface IJournalStreakService
{
    Task<JournalStreakSummaryResponseDto> GetStreakSummary(string userId, string? timezoneId, CancellationToken cancellationToken = default);
}

