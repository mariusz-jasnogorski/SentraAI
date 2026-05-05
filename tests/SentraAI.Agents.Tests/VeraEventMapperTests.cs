using Microsoft.Extensions.Options;
using SentraAI.Integrations;
using Xunit;

namespace SentraAI.Agents.Tests;

public sealed class VeraEventMapperTests
{
    [Fact]
    public void MapSDataJson_ShouldMapVeraDevicesToNormalizedEvents()
    {
        var json = """
        {
          "rooms": [
            { "id": 1, "name": "LivingRoom" }
          ],
          "devices": [
            {
              "id": 12,
              "name": "Living Room Temperature",
              "room": 1,
              "category": 17,
              "temperature": "21.5"
            },
            {
              "id": 13,
              "name": "Living Room Window",
              "room": 1,
              "category": 4,
              "tripped": "1"
            }
          ]
        }
        """;

        var mapper = new VeraEventMapper(Options.Create(new VeraOptions()));

        var events = mapper.MapSDataJson(json);

        Assert.Equal(2, events.Count);
        Assert.Contains(events, x => x.Type == "Temperature" && x.Value == "21.5" && x.Room == "LivingRoom");
        Assert.Contains(events, x => x.Type == "Window" && x.Value == "Open" && x.Room == "LivingRoom");
    }
}
