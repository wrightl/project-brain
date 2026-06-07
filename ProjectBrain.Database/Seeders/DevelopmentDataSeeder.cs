using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectBrain.Database.Constants;
using ProjectBrain.Database.Interfaces;
using ProjectBrain.Database.Models;

namespace ProjectBrain.Database.Seeders;

public class DevelopmentDataSeeder(AppDbContext context, ILogger<DevelopmentDataSeeder> logger) : IDevelopmentDataSeeder
{
    public async Task<SeedUserDataResult> SeedUserDataAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.Trim().ToLower(), cancellationToken);

        if (user == null)
            throw new InvalidOperationException($"User not found for email: {userEmail}.");

        if (!user.IsOnboarded)
            throw new InvalidOperationException("User must be onboarded before seeding data.");

        var userId = user.Id;
        var now = DateTime.UtcNow;

        // Real-world goal messages (3 per day, varied)
        var goalTemplates = new[]
        {
            new[] { "Go for a 30 minute walk", "Finish the weekly report", "Call a friend or family member" },
            new[] { "Meditate for 10 minutes", "No screens after 9pm", "Prep lunch for tomorrow" },
            new[] { "Read for 20 minutes", "Drink 2 litres of water", "Write down 3 things I'm grateful for" },
            new[] { "Do 15 minutes of stretching", "Reply to overdue emails", "Early night – in bed by 10:30" },
            new[] { "Take a proper lunch break away from desk", "Send one message to someone I've lost touch with", "Tidy one room" },
            new[] { "Listen to a podcast or audiobook", "Cook a proper dinner (no takeaways)", "Spend 10 minutes planning tomorrow" },
            new[] { "Get outside in daylight", "Unsubscribe from 3 email lists", "No social media before noon" },
        };

        // Streak: 14 days of goals. Days 0–6 (today back 6 days) = all 3 completed → current streak 7.
        // Days 7–13 = mixed completion so streak was broken; then a block of 5 full days (e.g. 9–13) for longest streak 7 (or we make 8–12 all complete so longest is 5 then gap then 7).
        // Simpler: days 0 to 6 all complete (7-day current streak). Days 7–13: 7–9 all complete (3 days), 10 one incomplete, 11–13 all complete → longest streak 7, current 7.
        var goals = new List<Goal>();
        for (var d = 0; d < 14; d++)
        {
            var date = DateOnly.FromDateTime(now.AddDays(-d));
            var template = goalTemplates[d % goalTemplates.Length];
            // Days 0–6: all completed (current streak 7). Days 7–13: day 10 has one uncompleted; rest all completed so we get a 3-day run and a 3-day run, longest 3. So longest streak would be 7 from today.
            var allCompletedForDay = d <= 6 || d != 10;
            var dayBase = now.AddDays(-d);
            for (var i = 0; i < 3; i++)
            {
                var completed = allCompletedForDay;
                goals.Add(new Goal
                {
                    UserId = userId,
                    Date = date,
                    Index = i,
                    Message = template[i],
                    Completed = completed,
                    CompletedAt = completed ? dayBase.AddHours(9 + i) : null,
                    CreatedAt = dayBase,
                    UpdatedAt = now
                });
            }
        }

        await context.Goals.AddRangeAsync(goals, cancellationToken);

        // Tags (user-scoped) – real-world style
        var tag1 = new Tag { Id = Guid.NewGuid(), UserId = userId, Name = "Gratitude", CreatedAt = now };
        var tag2 = new Tag { Id = Guid.NewGuid(), UserId = userId, Name = "Sleep", CreatedAt = now };
        var tag3 = new Tag { Id = Guid.NewGuid(), UserId = userId, Name = "Anxiety", CreatedAt = now };
        await context.Tags.AddRangeAsync([tag1, tag2, tag3], cancellationToken);

        // Real-world journal entries with varied content
        var systemTags = await context.SystemTags.Take(3).ToListAsync(cancellationToken);
        var journalEntries = new List<JournalEntry>();
        var journalContents = new[]
        {
            (Content: """Today was one of those days where nothing went wrong but I still felt flat. Work was fine, the weather was fine. I think I'm just tired and need to protect my sleep better. Going to try the no screens after 9pm goal again – I always slip on that one. Something that did help: talking to Sarah at lunch instead of eating at my desk.""", Summary: "Reflecting on an okay day and sleep habits."),
            (Content: """Slept really badly – woke at 3am and couldn't drop off. Had a lot on my mind about the meeting today. In the end the meeting was fine and I was more prepared than I gave myself credit for. Trying to remember that the middle-of-the-night spiral is usually worse than reality. Grateful that my partner made coffee this morning.""", Summary: "Poor sleep and pre-meeting anxiety; meeting went okay."),
            (Content: """Morning pages really helped today. Wrote three pages of nonsense and then suddenly had a clear idea about how to approach the project. I've been stuck for days. Also made time for a walk at lunch – only 20 minutes but I came back feeling like a different person. Small wins.""", Summary: "Morning pages and a lunch walk made a difference."),
            (Content: """Not a great day mood-wise. Felt anxious from the moment I woke up. Did the 10 min meditation anyway and it took the edge off a bit. Didn't hit all my goals but I'm trying to be kind to myself – one bad day doesn't undo the streak. Tomorrow I'll focus on the basics: sleep, water, one thing I enjoy.""", Summary: "Anxious day; meditation helped; being gentle with self."),
            (Content: """Really grateful today. Had a proper catch-up with an old friend I've been meaning to message for months. We only talked for 20 minutes but it reminded me how much I value those connections. Also finally unsubscribed from a bunch of newsletters that were just noise. Feels like a small win for my attention.""", Summary: "Gratitude for reconnecting and clearing inbox."),
        };

        for (var j = 0; j < journalContents.Length; j++)
        {
            var (content, summary) = journalContents[j];
            var entryId = Guid.NewGuid();
            var entryDate = now.AddDays(-j);
            var entry = new JournalEntry
            {
                Id = entryId,
                UserId = userId,
                Content = content,
                Summary = summary,
                CreatedAt = entryDate,
                UpdatedAt = now
            };
            journalEntries.Add(entry);

            context.JournalEntryTags.Add(new JournalEntryTag
            {
                Id = Guid.NewGuid(),
                JournalEntryId = entryId,
                TagId = j % 3 == 0 ? tag1.Id : (j % 3 == 1 ? tag2.Id : tag3.Id),
                CreatedAt = now
            });

            if (systemTags.Count > 0)
            {
                context.JournalEntrySystemTags.Add(new JournalEntrySystemTag
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = entryId,
                    SystemTagId = systemTags[j % systemTags.Count].Id,
                    ResponsesJson = "{}",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await context.JournalEntries.AddRangeAsync(journalEntries, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded user data for {Email}: {Goals} goals, {Entries} journal entries, {Tags} tags",
            userEmail, goals.Count, journalEntries.Count, 3);

        return new SeedUserDataResult(GoalsCreated: goals.Count, JournalEntriesCreated: journalEntries.Count, TagsCreated: 3);
    }

    public async Task<SeedCoachDataResult> SeedCoachDataAsync(string coachUserEmail, string? clientUserEmail = null, CancellationToken cancellationToken = default)
    {
        var coachUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == coachUserEmail.Trim().ToLower(), cancellationToken);

        if (coachUser == null)
            throw new InvalidOperationException($"User not found for email: {coachUserEmail}.");

        if (!coachUser.IsOnboarded)
            throw new InvalidOperationException("User must be onboarded before seeding coach data.");

        User? clientUser = null;
        if (clientUserEmail != null)
        {
            clientUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == clientUserEmail.Trim().ToLower(), cancellationToken);
            if (clientUser == null)
                throw new InvalidOperationException($"Client user not found for email: {clientUserEmail}.");
            if (!clientUser.IsOnboarded)
                throw new InvalidOperationException("Client user must be onboarded before seeding connection data.");
        }

        var coachUserId = coachUser.Id;
        var profileCreated = false;
        CoachProfile profile;

        var existingProfile = await context.CoachProfiles.FirstOrDefaultAsync(p => p.UserId == coachUserId, cancellationToken);
        if (existingProfile != null)
        {
            profile = existingProfile;
        }
        else
        {
            profile = new CoachProfile
            {
                UserId = coachUserId,
                Bio = "Sample coach bio for development.",
                AvailabilityStatus = AvailabilityStatus.Available
            };
            await context.CoachProfiles.AddAsync(profile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            profileCreated = true;
        }

        var qualCount = 0;
        if (!await context.CoachQualifications.AnyAsync(q => q.CoachProfileId == profile.Id, cancellationToken))
        {
            await context.CoachQualifications.AddRangeAsync(
                [
                    new CoachQualification { CoachProfileId = profile.Id, Qualification = "Sample qualification 1" },
                    new CoachQualification { CoachProfileId = profile.Id, Qualification = "Sample qualification 2" }
                ],
                cancellationToken);
            qualCount = 2;
        }

        var specCount = 0;
        if (!await context.CoachSpecialisms.AnyAsync(s => s.CoachProfileId == profile.Id, cancellationToken))
        {
            await context.CoachSpecialisms.AddRangeAsync(
                [
                    new CoachSpecialism { CoachProfileId = profile.Id, Specialism = "Anxiety" },
                    new CoachSpecialism { CoachProfileId = profile.Id, Specialism = "Stress" }
                ],
                cancellationToken);
            specCount = 2;
        }

        var ageCount = 0;
        if (!await context.CoachAgeGroups.AnyAsync(a => a.CoachProfileId == profile.Id, cancellationToken))
        {
            await context.CoachAgeGroups.AddRangeAsync(
                [
                    new CoachAgeGroup { CoachProfileId = profile.Id, AgeGroup = "Adults" },
                    new CoachAgeGroup { CoachProfileId = profile.Id, AgeGroup = "Young adults" }
                ],
                cancellationToken);
            ageCount = 2;
        }

        var connectionsCreated = 0;
        var messagesCreated = 0;

        if (clientUser != null)
        {
            var existingConnection = await context.Connections
                .FirstOrDefaultAsync(c => c.UserId == clientUser.Id && c.CoachId == coachUserId, cancellationToken);

            if (existingConnection == null)
            {
                var connection = new Connection
                {
                    UserId = clientUser.Id,
                    CoachId = coachUserId,
                    Status = "accepted",
                    RequestedBy = AppRoles.User,
                    Message = "Sample connection request",
                    RequestedAt = DateTime.UtcNow.AddDays(-1),
                    RespondedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await context.Connections.AddAsync(connection, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                connectionsCreated = 1;

                // Add a couple of sample coach messages
                var msg1 = new CoachMessage
                {
                    UserId = clientUser.Id,
                    CoachId = coachUserId,
                    ConnectionId = connection.Id,
                    SenderId = coachUserId,
                    MessageType = "text",
                    Content = "Sample coach message: Hello, how can I help today?",
                    Status = "sent",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                };
                var msg2 = new CoachMessage
                {
                    UserId = clientUser.Id,
                    CoachId = coachUserId,
                    ConnectionId = connection.Id,
                    SenderId = clientUser.Id,
                    MessageType = "text",
                    Content = "Sample client reply: Thanks, just checking in.",
                    Status = "sent",
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                };
                await context.CoachMessages.AddRangeAsync([msg1, msg2], cancellationToken);
                messagesCreated = 2;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded coach data for {Email}: profile={Profile}, qualifications={Qual}, specialisms={Spec}, ageGroups={Age}, connections={Conn}, messages={Msg}",
            coachUserEmail, profileCreated, qualCount, specCount, ageCount, connectionsCreated, messagesCreated);

        return new SeedCoachDataResult(profileCreated, qualCount, specCount, ageCount, connectionsCreated, messagesCreated);
    }
}
