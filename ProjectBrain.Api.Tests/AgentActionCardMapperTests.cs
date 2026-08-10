using FluentAssertions;
using System.Text.Json;
using ProjectBrain.Api.Services;

namespace ProjectBrain.Api.Tests;

public class AgentActionCardMapperTests
{
    [Fact]
    public void MapToolResult_SearchCoaches_IncludesConnectionFieldsInCard()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "success": true,
              "coaches": [
                {
                  "coachProfileId": "42",
                  "name": "Sarah Coach",
                  "bio": "Bio",
                  "connectionStatus": "connected",
                  "connectionId": "conn-guid"
                }
              ]
            }
            """);

        var cards = AgentActionCardMapper
            .MapToolResult("search_coaches", doc.RootElement.Clone(), success: true)
            .ToList();

        cards.Should().HaveCount(1);
        var cardJson = JsonSerializer.Serialize(cards[0]);
        cardJson.Should().Contain("coaches_found");
        cardJson.Should().Contain("connectionStatus");
        cardJson.Should().Contain("conn-guid");
        cardJson.Should().Contain("Sarah Coach");
    }

    [Fact]
    public void MapToolResult_GetConnectedCoaches_UsesCoachesFoundCard()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "success": true,
              "coaches": [
                {
                  "coachProfileId": "7",
                  "name": "Alex Coach",
                  "connectionStatus": "connected",
                  "connectionId": "conn-2"
                }
              ]
            }
            """);

        var cards = AgentActionCardMapper
            .MapToolResult("get_connected_coaches", doc.RootElement.Clone(), success: true)
            .ToList();

        cards.Should().HaveCount(1);
        var cardJson = JsonSerializer.Serialize(cards[0]);
        cardJson.Should().Contain("coaches_found");
        cardJson.Should().Contain("Alex Coach");
    }
}
