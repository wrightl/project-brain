using Microsoft.Extensions.Configuration;
using ProjectBrain.Database;
using ProjectBrain.Database.Constants;

namespace ProjectBrain.Domain;

public class FakeCoachAutoAcceptService : IFakeCoachAutoAcceptService
{
    private readonly IConnectionService _connectionService;
    private readonly IConfiguration _configuration;

    public FakeCoachAutoAcceptService(
        IConnectionService connectionService,
        IConfiguration configuration)
    {
        _connectionService = connectionService;
        _configuration = configuration;
    }

    public async Task<Connection?> TryAutoAcceptAsync(Connection connection, string? coachEmail)
    {
        if (!FakeCoachEnvironment.IsEnabled(_configuration))
            return null;

        if (!TestUsers.IsTestCoachEmail(coachEmail))
            return null;

        if (connection.Status != "pending")
            return null;

        var accepted = await _connectionService.AcceptConnectionAsync(connection.UserId, connection.CoachId);
        if (!accepted)
            return null;

        return await _connectionService.GetConnectionAsync(connection.UserId, connection.CoachId);
    }
}

public interface IFakeCoachAutoAcceptService
{
    Task<Connection?> TryAutoAcceptAsync(Connection connection, string? coachEmail);
}
