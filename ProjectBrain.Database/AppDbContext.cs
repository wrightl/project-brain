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

        logger.LogInformation("OnModelCreating completed");
    }

    // public DbSet<Movie> Movies => Set<Movie>();
    // public DbSet<Egg> Eggs => Set<Egg>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
}