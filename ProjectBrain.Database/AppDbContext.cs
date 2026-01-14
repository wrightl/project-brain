using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using ProjectBrain.Database.Models;

public class AppDbContext(DbContextOptions<AppDbContext> options, ILogger<AppDbContext> logger) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        logger.LogInformation("OnModelCreating");

        modelBuilder.Entity<Conversation>()
            .HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure User-UserRole relationship
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleName });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleName)
            .HasPrincipalKey(r => r.Name);

        // Configure CoachProfile relationships
        modelBuilder.Entity<CoachProfile>()
            .HasOne(cp => cp.User)
            .WithMany()
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CoachQualification>()
            .HasOne(cq => cq.CoachProfile)
            .WithMany(cp => cp.Qualifications)
            .HasForeignKey(cq => cq.CoachProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CoachSpecialism>()
            .HasOne(cs => cs.CoachProfile)
            .WithMany(cp => cp.Specialisms)
            .HasForeignKey(cs => cs.CoachProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CoachAgeGroup>()
            .HasOne(cag => cag.CoachProfile)
            .WithMany(cp => cp.AgeGroups)
            .HasForeignKey(cag => cag.CoachProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure UserProfile relationships
        modelBuilder.Entity<UserProfile>()
            .HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NeurodiverseTrait>()
            .HasOne(nt => nt.UserProfile)
            .WithMany(up => up.NeurodiverseTraits)
            .HasForeignKey(nt => nt.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserPreference>()
            .HasOne(up => up.UserProfile)
            .WithOne(up => up.Preference)
            .HasForeignKey<UserPreference>(up => up.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Connection relationships
        // Note: Cannot use CASCADE on both foreign keys to the same table (Users)
        // Using CASCADE on UserId and NO ACTION on CoachId to avoid cascade path cycles
        modelBuilder.Entity<Connection>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Connection>()
            .HasOne(c => c.Coach)
            .WithMany()
            .HasForeignKey(c => c.CoachId)
            .OnDelete(DeleteBehavior.NoAction);

        // Unique constraint to prevent duplicate connections
        modelBuilder.Entity<Connection>()
            .HasIndex(c => new { c.UserId, c.CoachId })
            .IsUnique();

        // Indexes for efficient queries
        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.UserId);

        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.CoachId);

        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.Status);

        // Configure VoiceNote relationships
        modelBuilder.Entity<VoiceNote>()
            .HasIndex(vn => vn.UserId);

        modelBuilder.Entity<VoiceNote>()
            .HasIndex(vn => vn.CreatedAt);

        // Configure VoiceNote Duration precision
        modelBuilder.Entity<VoiceNote>()
            .Property(vn => vn.Duration)
            .HasPrecision(10, 2);

        // Configure Quiz relationships
        modelBuilder.Entity<QuizQuestion>()
            .HasOne(qq => qq.Quiz)
            .WithMany(q => q.Questions)
            .HasForeignKey(qq => qq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuizQuestion>()
            .HasIndex(qq => qq.QuizId);

        modelBuilder.Entity<QuizQuestion>()
            .HasIndex(qq => new { qq.QuizId, qq.QuestionOrder })
            .IsUnique();

        // Configure QuizQuestion decimal precision
        modelBuilder.Entity<QuizQuestion>()
            .Property(qq => qq.MinValue)
            .HasPrecision(18, 2);

        modelBuilder.Entity<QuizQuestion>()
            .Property(qq => qq.MaxValue)
            .HasPrecision(18, 2);

        // Configure QuizResponse relationships
        modelBuilder.Entity<QuizResponse>()
            .HasOne(qr => qr.Quiz)
            .WithMany()
            .HasForeignKey(qr => qr.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuizResponse>()
            .HasIndex(qr => qr.QuizId);

        modelBuilder.Entity<QuizResponse>()
            .HasIndex(qr => qr.UserId);

        modelBuilder.Entity<QuizResponse>()
            .HasIndex(qr => qr.CompletedAt);

        // Index for fast lookups (duplicate responses allowed)
        modelBuilder.Entity<QuizResponse>()
            .HasIndex(qr => new { qr.QuizId, qr.UserId });

        // Configure QuizResponse decimal precision
        modelBuilder.Entity<QuizResponse>()
            .Property(qr => qr.Score)
            .HasPrecision(18, 2);

        // Configure SubscriptionTier relationships
        modelBuilder.Entity<UserSubscription>()
            .HasOne(us => us.User)
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSubscription>()
            .HasOne(us => us.Tier)
            .WithMany()
            .HasForeignKey(us => us.TierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for UserSubscription
        modelBuilder.Entity<UserSubscription>()
            .HasIndex(us => new { us.UserId, us.UserType });

        modelBuilder.Entity<UserSubscription>()
            .HasIndex(us => new { us.Status, us.UserType });

        // Configure UsageTracking relationships
        modelBuilder.Entity<UsageTracking>()
            .HasOne(ut => ut.User)
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for UsageTracking (composite for efficient queries)
        modelBuilder.Entity<UsageTracking>()
            .HasIndex(ut => new { ut.UserId, ut.UsageType, ut.PeriodType, ut.PeriodStart });

        // Configure FileStorageUsage relationships
        modelBuilder.Entity<FileStorageUsage>()
            .HasOne(fsu => fsu.User)
            .WithMany()
            .HasForeignKey(fsu => fsu.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ResearchReport relationships
        modelBuilder.Entity<ResearchReport>()
            .HasOne(rr => rr.User)
            .WithMany()
            .HasForeignKey(rr => rr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResearchReport>()
            .HasIndex(rr => rr.UserId);

        // Configure ExternalIntegration relationships
        modelBuilder.Entity<ExternalIntegration>()
            .HasOne(ei => ei.User)
            .WithMany()
            .HasForeignKey(ei => ei.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExternalIntegration>()
            .HasIndex(ei => new { ei.UserId, ei.IntegrationType })
            .IsUnique();

        // Configure CoachMessage relationships
        // Note: Using NO ACTION for UserId to avoid cascade path cycles with Connections table
        modelBuilder.Entity<CoachMessage>()
            .HasOne(cm => cm.User)
            .WithMany()
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CoachMessage>()
            .HasOne(cm => cm.Coach)
            .WithMany()
            .HasForeignKey(cm => cm.CoachId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CoachMessage>()
            .HasOne(cm => cm.Sender)
            .WithMany()
            .HasForeignKey(cm => cm.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<CoachMessage>()
            .HasOne(cm => cm.Connection)
            .WithMany()
            .HasForeignKey(cm => cm.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CoachMessage>()
            .HasIndex(cm => new { cm.UserId, cm.CoachId, cm.CreatedAt });

        modelBuilder.Entity<CoachMessage>()
            .HasIndex(cm => new { cm.SenderId, cm.CreatedAt });

        // Configure SubscriptionExclusion relationships
        modelBuilder.Entity<SubscriptionExclusion>()
            .HasOne(se => se.User)
            .WithMany()
            .HasForeignKey(se => se.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubscriptionExclusion>()
            .HasOne(se => se.ExcludedByUser)
            .WithMany()
            .HasForeignKey(se => se.ExcludedBy)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<SubscriptionExclusion>()
            .HasIndex(se => new { se.UserId, se.UserType })
            .IsUnique();

        // Configure SubscriptionSettings relationships
        modelBuilder.Entity<SubscriptionSettings>()
            .HasOne(ss => ss.UpdatedByUser)
            .WithMany()
            .HasForeignKey(ss => ss.UpdatedBy)
            .OnDelete(DeleteBehavior.NoAction);

        // Configure AvailabilityStatus enum to be stored as string
        modelBuilder.Entity<CoachProfile>()
            .Property(cp => cp.AvailabilityStatus)
            .HasConversion<string>();

        // Configure JournalEntry relationships
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => je.UserId);

        modelBuilder.Entity<JournalEntry>()
            .HasIndex(je => je.CreatedAt);

        // Configure Tag relationships
        // Note: Using NO ACTION instead of CASCADE to avoid cascade path cycles
        // Tags are deleted via application logic since JournalEntryTags are cleaned up via JournalEntries
        modelBuilder.Entity<Tag>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Tag>()
            .HasIndex(t => new { t.UserId, t.Name })
            .IsUnique();

        // Configure JournalEntryTag many-to-many relationship
        modelBuilder.Entity<JournalEntryTag>()
            .HasOne(jet => jet.JournalEntry)
            .WithMany(je => je.JournalEntryTags)
            .HasForeignKey(jet => jet.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JournalEntryTag>()
            .HasOne(jet => jet.Tag)
            .WithMany(t => t.JournalEntryTags)
            .HasForeignKey(jet => jet.TagId)
            .OnDelete(DeleteBehavior.NoAction);

        // Unique constraint to prevent duplicate tag assignments
        modelBuilder.Entity<JournalEntryTag>()
            .HasIndex(jet => new { jet.JournalEntryId, jet.TagId })
            .IsUnique();

        // Configure CoachRating relationships
        modelBuilder.Entity<CoachRating>()
            .HasOne(cr => cr.User)
            .WithMany()
            .HasForeignKey(cr => cr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CoachRating>()
            .HasOne(cr => cr.Coach)
            .WithMany()
            .HasForeignKey(cr => cr.CoachId)
            .OnDelete(DeleteBehavior.NoAction);

        // Unique constraint to prevent duplicate ratings (one rating per user per coach)
        modelBuilder.Entity<CoachRating>()
            .HasIndex(cr => new { cr.UserId, cr.CoachId })
            .IsUnique();

        // Indexes for efficient queries
        modelBuilder.Entity<CoachRating>()
            .HasIndex(cr => cr.CoachId);

        modelBuilder.Entity<CoachRating>()
            .HasIndex(cr => cr.CreatedAt);

        // Configure Goal relationships
        modelBuilder.Entity<Goal>()
            .HasIndex(g => new { g.UserId, g.Date });

        modelBuilder.Entity<Goal>()
            .HasIndex(g => g.UserId);

        // Unique constraint: one goal per user/date/index combination
        modelBuilder.Entity<Goal>()
            .HasIndex(g => new { g.UserId, g.Date, g.Index })
            .IsUnique();

        // Check constraint for Index (0-2)
        modelBuilder.Entity<Goal>()
            .HasCheckConstraint("CK_Goal_Index", "[Index] >= 0 AND [Index] <= 2");

        // Check constraint for Message length (max 500)
        modelBuilder.Entity<Goal>()
            .HasCheckConstraint("CK_Goal_MessageLength", "LEN([Message]) <= 500");

        // Configure OnboardingData relationships
        modelBuilder.Entity<OnboardingData>()
            .HasOne(od => od.User)
            .WithMany()
            .HasForeignKey(od => od.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OnboardingData>()
            .HasIndex(od => od.UserId)
            .IsUnique();

        // Configure DeviceToken relationships
        modelBuilder.Entity<DeviceToken>()
            .HasOne(dt => dt.User)
            .WithMany()
            .HasForeignKey(dt => dt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on Token
        modelBuilder.Entity<DeviceToken>()
            .HasIndex(dt => dt.Token)
            .IsUnique();

        // Index on UserId and IsActive for efficient queries
        modelBuilder.Entity<DeviceToken>()
            .HasIndex(dt => new { dt.UserId, dt.IsActive });

        // Index on LastValidatedAt for cleanup queries
        modelBuilder.Entity<DeviceToken>()
            .HasIndex(dt => dt.LastValidatedAt);

        logger.LogInformation("OnModelCreating completed");
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<CoachProfile> CoachProfiles => Set<CoachProfile>();
    public DbSet<CoachQualification> CoachQualifications => Set<CoachQualification>();
    public DbSet<CoachSpecialism> CoachSpecialisms => Set<CoachSpecialism>();
    public DbSet<CoachAgeGroup> CoachAgeGroups => Set<CoachAgeGroup>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<NeurodiverseTrait> NeurodiverseTraits => Set<NeurodiverseTrait>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<VoiceNote> VoiceNotes => Set<VoiceNote>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizResponse> QuizResponses => Set<QuizResponse>();
    public DbSet<SubscriptionTier> SubscriptionTiers => Set<SubscriptionTier>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<UsageTracking> UsageTrackings => Set<UsageTracking>();
    public DbSet<FileStorageUsage> FileStorageUsages => Set<FileStorageUsage>();
    public DbSet<ResearchReport> ResearchReports => Set<ResearchReport>();
    public DbSet<ExternalIntegration> ExternalIntegrations => Set<ExternalIntegration>();
    public DbSet<CoachMessage> CoachMessages => Set<CoachMessage>();
    public DbSet<SubscriptionExclusion> SubscriptionExclusions => Set<SubscriptionExclusion>();
    public DbSet<SubscriptionSettings> SubscriptionSettings => Set<SubscriptionSettings>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<JournalEntryTag> JournalEntryTags => Set<JournalEntryTag>();
    public DbSet<CoachRating> CoachRatings => Set<CoachRating>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<OnboardingData> OnboardingData => Set<OnboardingData>();
    public DbSet<AgentWorkflow> AgentWorkflows => Set<AgentWorkflow>();
    public DbSet<AgentAction> AgentActions => Set<AgentAction>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
}