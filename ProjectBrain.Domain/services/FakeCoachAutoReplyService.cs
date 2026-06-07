using Microsoft.Extensions.Configuration;
using ProjectBrain.Database;
using ProjectBrain.Database.Constants;
using ProjectBrain.Domain.Repositories;

namespace ProjectBrain.Domain;

public class FakeCoachAutoReplyService : IFakeCoachAutoReplyService
{
    private readonly ICoachMessageService _coachMessageService;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public FakeCoachAutoReplyService(
        ICoachMessageService coachMessageService,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _coachMessageService = coachMessageService;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<CoachMessage?> TryCreateAutoReplyAsync(
        Connection connection,
        string senderId,
        DateTime userMessageCreatedAt)
    {
        if (!FakeCoachEnvironment.IsEnabled(_configuration))
            return null;

        if (connection.Status != "accepted")
            return null;

        if (senderId != connection.UserId)
            return null;

        var coach = await _userRepository.GetByIdAsync(connection.CoachId);
        if (coach == null || !TestUsers.IsTestCoachEmail(coach.Email))
            return null;

        var messageText = _configuration["FakeCoachAutoReply:Message"]
            ?? FakeCoachEnvironment.DefaultMessage;

        var autoReply = new CoachMessage
        {
            UserId = connection.UserId,
            CoachId = connection.CoachId,
            ConnectionId = connection.Id,
            SenderId = connection.CoachId,
            MessageType = "text",
            Content = messageText,
            Status = "sent",
            CreatedAt = userMessageCreatedAt.AddMilliseconds(1)
        };

        var savedMessage = await _coachMessageService.Add(autoReply);
        return await _coachMessageService.GetById(savedMessage.Id);
    }
}

public interface IFakeCoachAutoReplyService
{
    Task<CoachMessage?> TryCreateAutoReplyAsync(
        Connection connection,
        string senderId,
        DateTime userMessageCreatedAt);
}
