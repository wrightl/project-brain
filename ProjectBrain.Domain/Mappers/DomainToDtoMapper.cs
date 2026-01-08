
using Microsoft.EntityFrameworkCore;

using ProjectBrain.Database.Models;

namespace ProjectBrain.Domain.Mappers;

public static class DomainToDtoMapper
{
    public static BaseUserDto ToBaseUserDto(this User user)
    {
        return new BaseUserDto
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
            Connection = user.Connection,
            EmailVerified = user.EmailVerified,
            Roles = user.UserRoles?.Select(ur => ur.RoleName)?.ToList() ?? new List<string>()
        };
    }

    // public static UserDto ToUserDto(this User user)
    // {
    //     return new UserDto
    //     {
    //         Id = user.Id,
    //         Email = user.Email,
    //         FullName = user.FullName,
    //         IsOnboarded = user.IsOnboarded,
    //         LastActivityAt = user.LastActivityAt,
    //         StreetAddress = user.StreetAddress,
    //         AddressLine2 = user.AddressLine2,
    //         City = user.City,
    //         StateProvince = user.StateProvince,
    //         PostalCode = user.PostalCode,
    //         Country = user.Country,
    //         Roles = user.UserRoles?.Select(ur => ur.RoleName)?.ToList() ?? new List<string>()
    //     };
    // }

    // public static CoachDto ToCoachDto(this User user)
    // {
    //     return new CoachDto
    //     {
    //         Id = user.Id,
    //         Email = user.Email,
    //         FullName = user.FullName,
    //         IsOnboarded = user.IsOnboarded,
    //         LastActivityAt = user.LastActivityAt,
    //         StreetAddress = user.StreetAddress,
    //         AddressLine2 = user.AddressLine2,
    //         City = user.City,
    //         StateProvince = user.StateProvince,
    //         PostalCode = user.PostalCode,
    //         Country = user.Country,
    //         Roles = user.UserRoles?.Select(ur => ur.RoleName)?.ToList() ?? new List<string>(),
    //         Qualifications = new List<string>(),
    //         Specialisms = new List<string>(),
    //         AgeGroups = new List<string>(),
    //         AvailabilityStatus = null
    //     };
    // }

    public static UserDto ToUserDto(this UserProfile userProfile)
    {
        if (userProfile.User == null)
        {
            throw new InvalidOperationException("UserProfile must include User to convert to UserDto");
        }
        return new UserDto
        {
            Id = userProfile.User.Id,
            Email = userProfile.User.Email,
            FullName = userProfile.User.FullName,
            IsOnboarded = userProfile.User.IsOnboarded,
            LastActivityAt = userProfile.User.LastActivityAt,
            StreetAddress = userProfile.User.StreetAddress,
            AddressLine2 = userProfile.User.AddressLine2,
            City = userProfile.User.City,
            StateProvince = userProfile.User.StateProvince,
            PostalCode = userProfile.User.PostalCode,
            Country = userProfile.User.Country,
            Connection = userProfile.User.Connection,
            EmailVerified = userProfile.User.EmailVerified,
            Roles = userProfile.User.UserRoles?.Select(ur => ur.RoleName)?.ToList() ?? new List<string>(),
            DoB = userProfile.DoB,
            PreferredPronoun = userProfile.PreferredPronoun,
            NeurodiverseTraits = userProfile.NeurodiverseTraits?.Select(t => t.Trait).ToList() ?? new List<string>(),
            Preferences = userProfile.Preference?.Preferences
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
            Id = coachProfile.UserId,
            CoachProfileId = coachProfile.Id.ToString(),
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
            Connection = coachProfile.User.Connection,
            EmailVerified = coachProfile.User.EmailVerified,
            Roles = coachProfile.User.UserRoles?.Select(ur => ur.RoleName)?.ToList() ?? new List<string>(),
            Qualifications = coachProfile.Qualifications?.Select(q => q.Qualification).ToList() ?? new List<string>(),
            Specialisms = coachProfile.Specialisms?.Select(s => s.Specialism).ToList() ?? new List<string>(),
            AgeGroups = coachProfile.AgeGroups?.Select(ag => ag.AgeGroup).ToList() ?? new List<string>(),
            AvailabilityStatus = coachProfile.AvailabilityStatus
        };
    }

    /// <summary>
    /// Sets the AvailabilityStatus for a coach based on their recent activity, manual status, and chat activity.
    /// </summary>
    public static async Task<CoachDto> SetOnlineStatusAsync(
        this CoachDto coachDto,
        IUserActivityService userActivityService,
        ICoachMessageService coachMessageService,
        int activityWindowMinutes = 30)
    {
        try
        {
            var isActive = await userActivityService.IsUserActiveAsync(coachDto.Id, activityWindowMinutes);

            // Determine availability status
            // Auto-determine status if not manually set
            if (!isActive)
            {
                coachDto.AvailabilityStatus = AvailabilityStatus.Offline;
            }
            else
            {
                // Check if idle for more than 30 minutes
                var lastActivity = coachDto.LastActivityAt;
                if (lastActivity.HasValue)
                {
                    var idleMinutes = (DateTime.UtcNow - lastActivity.Value).TotalMinutes;
                    if (idleMinutes > 30)
                    {
                        coachDto.AvailabilityStatus = AvailabilityStatus.Away;
                    }
                    else
                    {
                        // Check if in active chat
                        if (coachMessageService != null)
                        {
                            var recentChatCutoff = DateTime.UtcNow.AddMinutes(-10);
                            var hasActiveChat = (await coachMessageService.GetByCoachId(coachDto.Id))
                                .Any(cm => cm.CreatedAt >= recentChatCutoff);

                            coachDto.AvailabilityStatus = hasActiveChat ? AvailabilityStatus.Busy : AvailabilityStatus.Available;
                        }
                        else
                        {
                            coachDto.AvailabilityStatus = AvailabilityStatus.Available;
                        }
                    }
                }
                else
                {
                    coachDto.AvailabilityStatus = AvailabilityStatus.Available;
                }
            }
        }
        catch
        {
            // If we can't determine online status, default to false and Offline
            if (!coachDto.AvailabilityStatus.HasValue)
            {
                coachDto.AvailabilityStatus = AvailabilityStatus.Offline;
            }
        }

        return coachDto;
    }

    /// <summary>
    /// Sets the IsOnline status and AvailabilityStatus for multiple coaches based on their recent activity.
    /// More efficient than calling SetOnlineStatusAsync for each coach individually.
    /// Uses batch checking for better performance.
    /// </summary>
    public static async Task<List<CoachDto>> SetOnlineStatusAsync(
        this List<CoachDto> coachDtos,
        IUserActivityService userActivityService,
        ICoachMessageService coachMessageService,
        int activityWindowMinutes = 30)
    {
        if (coachDtos == null || !coachDtos.Any())
            return coachDtos ?? new List<CoachDto>();

        try
        {
            // Batch check all coaches at once for better performance
            var tasks = coachDtos.Select(async coachDto =>
            {
                try
                {
                    await coachDto.SetOnlineStatusAsync(userActivityService, coachMessageService, activityWindowMinutes);
                }
                catch
                {
                    if (!coachDto.AvailabilityStatus.HasValue)
                    {
                        coachDto.AvailabilityStatus = AvailabilityStatus.Offline;
                    }
                }
            });

            await Task.WhenAll(tasks);
        }
        catch
        {
            // If we can't determine online status, default all to false
            foreach (var coachDto in coachDtos)
            {
                if (!coachDto.AvailabilityStatus.HasValue)
                {
                    coachDto.AvailabilityStatus = AvailabilityStatus.Offline;
                }
            }
        }

        return coachDtos;
    }
}