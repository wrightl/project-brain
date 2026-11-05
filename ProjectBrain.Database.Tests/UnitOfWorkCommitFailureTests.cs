using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class UnitOfWorkCommitFailureTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenSaveFails_PropagatesSaveException()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(new ThrowOnSaveInterceptor())
            .Options;

        await using var context = new AppDbContext(options, Mock.Of<ILogger<AppDbContext>>());
        var unitOfWork = new UnitOfWork(context);

        var act = () => unitOfWork.ExecuteInTransactionAsync(() =>
        {
            context.Users.Add(new User
            {
                Id = "user-1",
                Email = "user@example.com",
                FullName = "User"
            });
            return Task.CompletedTask;
        });

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Be("simulated save failure");
    }

    private sealed class ThrowOnSaveInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
            => throw new InvalidOperationException("simulated save failure");

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated save failure");
    }
}
