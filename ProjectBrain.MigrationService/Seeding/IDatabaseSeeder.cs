namespace ProjectBrain.MigrationService.Seeding;

public interface IDatabaseSeeder
{
    Task SeedAllAsync(CancellationToken cancellationToken = default);
}
