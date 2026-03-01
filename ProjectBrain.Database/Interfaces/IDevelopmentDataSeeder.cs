namespace ProjectBrain.Database.Interfaces;

public interface IDevelopmentDataSeeder
{
    /// <summary>
    /// Seeds development data for the user identified by <paramref name="userEmail"/> (goals, journal entries, tags).
    /// User must exist and be onboarded.
    /// </summary>
    Task<SeedUserDataResult> SeedUserDataAsync(string userEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds development data for the coach identified by <paramref name="coachUserEmail"/> (profile, qualifications, specialisms, age groups).
    /// Optionally seeds a connection and sample messages if <paramref name="clientUserEmail"/> is provided.
    /// Coach and client (if provided) must exist and be onboarded.
    /// </summary>
    Task<SeedCoachDataResult> SeedCoachDataAsync(string coachUserEmail, string? clientUserEmail = null, CancellationToken cancellationToken = default);
}

public record SeedUserDataResult(int GoalsCreated, int JournalEntriesCreated, int TagsCreated);

public record SeedCoachDataResult(bool CoachProfileCreated, int QualificationsCreated, int SpecialismsCreated, int AgeGroupsCreated, int ConnectionsCreated, int CoachMessagesCreated);
