using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class CoachProfileServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CoachProfileService _service;
    private const string UserId = "auth0|coach-profile-test";

    public CoachProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);
        var unitOfWork = new UnitOfWork(_context);
        var specialisms = new Mock<ICoachSpecialismOptionService>();
        specialisms
            .Setup(s => s.ValidateSpecialismsAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new CoachProfileService(
            new CoachProfileRepository(_context),
            _context,
            unitOfWork,
            specialisms.Object);

        _context.Users.Add(new User
        {
            Id = UserId,
            Email = "coach@test.com",
            FullName = "Coach Test"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateOrUpdate_BioOnly_ShouldPreserveCollections()
    {
        await _service.CreateOrUpdate(
            UserId,
            qualifications: ["MSc Psychology"],
            specialisms: ["ADHD"],
            ageGroups: ["Adults"],
            bio: "Original bio");

        // PUT /coaches/me omits empty arrays and often sends only bio/imageUrl.
        await _service.CreateOrUpdate(UserId, bio: "Updated bio");

        var profileId = await _context.CoachProfiles.AsNoTracking()
            .Where(p => p.UserId == UserId)
            .Select(p => p.Id)
            .FirstAsync();

        var qualifications = await _context.CoachQualifications.AsNoTracking()
            .Where(q => q.CoachProfileId == profileId)
            .Select(q => q.Qualification)
            .ToListAsync();
        var specialisms = await _context.CoachSpecialisms.AsNoTracking()
            .Where(s => s.CoachProfileId == profileId)
            .Select(s => s.Specialism)
            .ToListAsync();
        var ageGroups = await _context.CoachAgeGroups.AsNoTracking()
            .Where(a => a.CoachProfileId == profileId)
            .Select(a => a.AgeGroup)
            .ToListAsync();
        var bio = await _context.CoachProfiles.AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => p.Bio)
            .FirstAsync();

        qualifications.Should().BeEquivalentTo(["MSc Psychology"]);
        specialisms.Should().BeEquivalentTo(["ADHD"]);
        ageGroups.Should().BeEquivalentTo(["Adults"]);
        bio.Should().Be("Updated bio");
    }

    [Fact]
    public async Task CreateOrUpdate_EmptyQualifications_ShouldClearOnlyQualifications()
    {
        await _service.CreateOrUpdate(
            UserId,
            qualifications: ["MSc Psychology"],
            specialisms: ["ADHD"],
            ageGroups: ["Adults"]);

        await _service.CreateOrUpdate(UserId, qualifications: []);

        var profileId = await _context.CoachProfiles.AsNoTracking()
            .Where(p => p.UserId == UserId)
            .Select(p => p.Id)
            .FirstAsync();

        var qualifications = await _context.CoachQualifications.AsNoTracking()
            .Where(q => q.CoachProfileId == profileId)
            .Select(q => q.Qualification)
            .ToListAsync();
        var specialisms = await _context.CoachSpecialisms.AsNoTracking()
            .Where(s => s.CoachProfileId == profileId)
            .Select(s => s.Specialism)
            .ToListAsync();

        qualifications.Should().BeEmpty();
        specialisms.Should().BeEquivalentTo(["ADHD"]);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
