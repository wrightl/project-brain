

namespace ProjectBrain.Domain.Mappers;

public static class DomainToDtoMapper
{
    public static UserDto ToUserDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            IsOnboarded = user.IsOnboarded,
            LastActivityAt = user.LastActivityAt,
            StreetAddress = user.StreetAddress,
            AddressLine2 = user.AddressLine2,
            City = user.City,
            StateProvince = user.StateProvince,
            PostalCode = user.PostalCode,
            Country = user.Country,
            Roles = user.UserRoles?.Select(ur => ur.RoleName)?.ToList()
        };
    }

    public static CoachDto ToCoachDto(this CoachProfile coachProfile)
    {
        if (coachProfile.User == null)
        {
            throw new InvalidOperationException("CoachProfile must include User to convert to CoachDto");
        }

        return new CoachDto
        {
            Id = coachProfile.User.Id,
            Email = coachProfile.User.Email,
            FullName = coachProfile.User.FullName,
            IsOnboarded = coachProfile.User.IsOnboarded,
            LastActivityAt = coachProfile.User.LastActivityAt,
            StreetAddress = coachProfile.User.StreetAddress,
            AddressLine2 = coachProfile.User.AddressLine2,
            City = coachProfile.User.City,
            StateProvince = coachProfile.User.StateProvince,
            PostalCode = coachProfile.User.PostalCode,
            Country = coachProfile.User.Country,
            Roles = coachProfile.User.UserRoles?.Select(ur => ur.RoleName)?.ToList() ?? new List<string>(),
            Qualifications = coachProfile.Qualifications?.Select(q => q.Qualification).ToList() ?? new List<string>(),
            Specialisms = coachProfile.Specialisms?.Select(s => s.Specialism).ToList() ?? new List<string>(),
            AgeGroups = coachProfile.AgeGroups?.Select(ag => ag.AgeGroup).ToList() ?? new List<string>(),
            IsOnline = false // Will be set by SetOnlineStatus extension method
        };
    }

    /// <summary>
    /// Sets the IsOnline status for a coach based on their recent activity.
    /// A coach is considered online if they were active within the last 30 minutes.
    /// </summary>
    public static async Task<CoachDto> SetOnlineStatusAsync(
        this CoachDto coachDto,
        IUserActivityService userActivityService,
        int activityWindowMinutes = 30)
    {
        try
        {
            coachDto.IsOnline = await userActivityService.IsUserActiveAsync(coachDto.Id, activityWindowMinutes);
        }
        catch
        {
            // If we can't determine online status, default to false
            coachDto.IsOnline = false;
        }

        return coachDto;
    }

    /// <summary>
    /// Sets the IsOnline status for multiple coaches based on their recent activity.
    /// More efficient than calling SetOnlineStatusAsync for each coach individually.
    /// Uses batch checking for better performance.
    /// </summary>
    public static async Task<List<CoachDto>> SetOnlineStatusAsync(
        this List<CoachDto> coachDtos,
        IUserActivityService userActivityService,
        int activityWindowMinutes = 30)
    {
        if (coachDtos == null || !coachDtos.Any())
            return coachDtos;

        try
        {
            // Batch check all coaches at once for better performance
            var tasks = coachDtos.Select(async coachDto =>
            {
                try
                {
                    coachDto.IsOnline = await userActivityService.IsUserActiveAsync(coachDto.Id, activityWindowMinutes);
                }
                catch
                {
                    coachDto.IsOnline = false;
                }
            });

            await Task.WhenAll(tasks);
        }
        catch
        {
            // If we can't determine online status, default all to false
            foreach (var coachDto in coachDtos)
            {
                coachDto.IsOnline = false;
            }
        }

        return coachDtos;
    }
}