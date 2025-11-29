using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
}