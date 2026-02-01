using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class ReferralServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ReferralService _referralService;
    private readonly Mock<IEmailService> _emailService;

    public ReferralServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, dbLogger.Object);

        var referralLogger = new Mock<ILogger<ReferralService>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:BaseUrl"] = "https://example.test"
            })
            .Build();

        _emailService = new Mock<IEmailService>();

        var referralSettingsService = new Mock<IReferralSettingsService>();
        referralSettingsService
            .Setup(s => s.GetReferralSettingsAsync())
            .ReturnsAsync(new ReferralSettings
            {
                Enabled = true,
                MaxInvitesPerRequest = 10,
                InviteTokenExpiryDays = 30,
                InviteeFreeMonths = 1,
                InviterFreeMonths = 1,
                MaxRewardsPerInviter = 12,
                RequireInviterActiveSubscriberToEarn = false
            });

        var inviteRepository = new ReferralInviteRepository(_context);
        var rewardRepository = new ReferralRewardRepository(_context);
        var userRepository = new UserRepository(_context);
        var unitOfWork = new UnitOfWork(_context);

        var stripeService = new Mock<IStripeService>();

        _referralService = new ReferralService(
            referralLogger.Object,
            configuration,
            _emailService.Object,
            referralSettingsService.Object,
            inviteRepository,
            rewardRepository,
            userRepository,
            stripeService.Object,
            _context,
            unitOfWork);
    }

    [Fact]
    public async Task CreateInvitesAsync_ShouldSkipEmailsAlreadyAssociatedWithAnAccount()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Id = "auth0|existing",
            Email = "existing@example.com",
            FullName = "Existing User"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _referralService.CreateInvitesAsync(
            inviterUserId: "auth0|inviter",
            inviterEmail: "inviter@example.com",
            inviterName: "Inviter User",
            emails: new[] { "existing@example.com", "new@example.com" },
            baseUrl: "https://example.test");

        // Assert
        result.Created.Should().ContainSingle(i => i.RecipientEmail == "new@example.com");
        result.Skipped.Should().ContainSingle(s =>
            s.RecipientEmail == "existing@example.com" &&
            s.Reason.Contains("account", StringComparison.OrdinalIgnoreCase));

        var invites = await _context.ReferralInvites.AsNoTracking().ToListAsync();
        invites.Should().ContainSingle(i => i.RecipientEmailNormalized == "new@example.com");
        invites.Should().NotContain(i => i.RecipientEmailNormalized == "existing@example.com");

        _emailService.Verify(
            es => es.SendEmailAsync(
                It.Is<string>(to => to == "existing@example.com"),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>()),
            Times.Never);

        _emailService.Verify(
            es => es.SendEmailAsync(
                It.Is<string>(to => to == "new@example.com"),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, object>?>(),
                It.IsAny<List<string>?>(),
                It.IsAny<List<string>?>()),
            Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

