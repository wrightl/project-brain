namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using ProjectBrain.Database.Models;
using ProjectBrain.Domain.Exceptions;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class CoachRatingService : ICoachRatingService
{
    private readonly ICoachRatingRepository _repository;
    private readonly IConnectionService _connectionService;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public CoachRatingService(
        ICoachRatingRepository repository,
        IConnectionService connectionService,
        AppDbContext context,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _connectionService = connectionService;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<CoachRating> CreateOrUpdateRatingAsync(string userId, string coachId, int rating, string? feedback = null)
    {
        // Validate rating range
        if (rating < 1 || rating > 5)
        {
            throw new AppException("INVALID_RATING", "Rating must be between 1 and 5", 400);
        }

        // Check if user is connected to the coach
        var connection = await _connectionService.GetConnectionAsync(userId, coachId);
        if (connection == null || connection.Status != "accepted")
        {
            throw new AppException("NOT_CONNECTED", "You can only rate coaches you are connected to", 400);
        }

        // Check if rating already exists
        var existingRating = await _context.CoachRatings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CoachId == coachId);

        if (existingRating != null)
        {
            // Update existing rating
            existingRating.Rating = rating;
            existingRating.Feedback = feedback;
            existingRating.UpdatedAt = DateTime.UtcNow;

            _context.CoachRatings.Update(existingRating);
            await _unitOfWork.SaveChangesAsync();
            return existingRating;
        }

        // Create new rating
        var coachRating = new CoachRating
        {
            UserId = userId,
            CoachId = coachId,
            Rating = rating,
            Feedback = feedback,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CoachRatings.Add(coachRating);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return coachRating;
        }
        catch (DbUpdateException ex)
        {
            // Handle unique constraint violation (race condition)
            // Check if this is a unique constraint violation by examining the exception message
            // This works across different database providers
            var isUniqueConstraintViolation = ex.InnerException?.Message?.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message?.Contains("IX_CoachRatings_UserId_CoachId", StringComparison.OrdinalIgnoreCase) == true;

            if (isUniqueConstraintViolation)
            {
                // Another request created the rating between our check and save
                // Fetch and update the existing rating
                var preExistingRating = await _context.CoachRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.CoachId == coachId);

                if (preExistingRating != null)
                {
                    preExistingRating.Rating = rating;
                    preExistingRating.Feedback = feedback;
                    preExistingRating.UpdatedAt = DateTime.UtcNow;

                    _context.CoachRatings.Update(preExistingRating);
                    await _unitOfWork.SaveChangesAsync();
                    return preExistingRating;
                }
            }

            // If we still can't find it or it's not a unique constraint violation, rethrow
            throw;
        }
    }

    public async Task<CoachRating?> GetRatingAsync(string userId, string coachId)
    {
        return await _repository.GetByUserAndCoachAsync(userId, coachId);
    }

    public async Task<IEnumerable<CoachRating>> GetRatingsByCoachIdAsync(string coachId)
    {
        return await _repository.GetRatingsByCoachIdAsync(coachId);
    }

    public async Task<IEnumerable<CoachRating>> GetPagedRatingsByCoachIdAsync(string coachId, int skip, int take)
    {
        return await _repository.GetPagedRatingsByCoachIdAsync(coachId, skip, take);
    }

    public async Task<double?> GetAverageRatingAsync(string coachId)
    {
        return await _repository.GetAverageRatingByCoachIdAsync(coachId);
    }

    public async Task<int> GetRatingCountAsync(string coachId)
    {
        return await _repository.CountRatingsByCoachIdAsync(coachId);
    }
}

