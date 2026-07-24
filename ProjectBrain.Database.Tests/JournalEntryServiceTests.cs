using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Database.Tests;

public class JournalEntryServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly JournalEntryService _service;

    public JournalEntryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockLogger = new Mock<ILogger<AppDbContext>>();
        _context = new AppDbContext(options, mockLogger.Object);

        var transaction = new Mock<IDbContextTransaction>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        _unitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new JournalEntryService(
            new JournalEntryRepository(_context),
            new TagRepository(_context),
            _context,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Update_CommitsTransaction_SoEditsAreNotRolledBack()
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = "auth0|journal-user",
            Content = "Original content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();

        entry.Content = "Edited content that must persist";

        await _service.Update(entry);

        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        // SaveChanges alone is not enough: disposing an uncommitted EF transaction rolls back.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Add_CommitsTransaction()
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = "auth0|journal-user",
            Content = "New entry",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _service.Add(entry);

        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
