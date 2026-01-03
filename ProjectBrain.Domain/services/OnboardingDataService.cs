namespace ProjectBrain.Domain;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class OnboardingDataService : IOnboardingDataService
{
    private readonly IOnboardingDataRepository _repository;
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private const string OnboardingDataCacheKeyPrefix = "onboardingdata:";
    private static readonly TimeSpan OnboardingDataCacheExpiration = TimeSpan.FromMinutes(30);

    public OnboardingDataService(
        IOnboardingDataRepository repository,
        AppDbContext context,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<OnboardingData?> GetByUserId(string userId)
    {
        // Try cache first
        var cacheKey = $"{OnboardingDataCacheKeyPrefix}{userId}";
        var cachedData = await _cache.GetAsync<OnboardingData>(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }

        var data = await _repository.GetByUserIdAsync(userId);

        // Cache the data if found
        if (data != null)
        {
            await _cache.SetAsync(cacheKey, data, OnboardingDataCacheExpiration);
        }

        return data;
    }

    public async Task<OnboardingData> CreateOrUpdate(string userId, object onboardingData)
    {
        // Get tracked entity for potential update (not using AsNoTracking)
        var existingData = await _context.OnboardingData
            .FirstOrDefaultAsync(od => od.UserId == userId);

        var onboardingJson = JsonSerializer.Serialize(onboardingData, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        if (existingData == null)
        {
            // Create new onboarding data
            var newData = new OnboardingData
            {
                UserId = userId,
                OnboardingJson = onboardingJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _repository.Add(newData);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cache
            var cacheKey = $"{OnboardingDataCacheKeyPrefix}{userId}";
            await _cache.RemoveAsync(cacheKey);

            return newData;
        }
        else
        {
            // Update existing onboarding data
            existingData.OnboardingJson = onboardingJson;
            existingData.UpdatedAt = DateTime.UtcNow;

            _repository.Update(existingData);
            await _unitOfWork.SaveChangesAsync();

            // Invalidate cache
            var cacheKey = $"{OnboardingDataCacheKeyPrefix}{userId}";
            await _cache.RemoveAsync(cacheKey);

            return existingData;
        }
    }

    public async Task<bool> DeleteByUserId(string userId)
    {
        // Get tracked entity for deletion (not using AsNoTracking)
        var data = await _context.OnboardingData
            .FirstOrDefaultAsync(od => od.UserId == userId);
        
        if (data == null)
        {
            return false;
        }

        _repository.Remove(data);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        var cacheKey = $"{OnboardingDataCacheKeyPrefix}{userId}";
        await _cache.RemoveAsync(cacheKey);

        return true;
    }
}

public interface IOnboardingDataService
{
    Task<OnboardingData?> GetByUserId(string userId);
    Task<OnboardingData> CreateOrUpdate(string userId, object onboardingData);
    Task<bool> DeleteByUserId(string userId);
}

