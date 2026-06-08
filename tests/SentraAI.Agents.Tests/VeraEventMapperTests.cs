using SentraAI.Contracts;
using SentraAI.Integrations;
using Xunit;

namespace SentraAI.Agents.Tests;

public sealed class VeraEventMapperTests
{
    [Fact]
    public void Map_maps_temperature_device()
    {
        var device = new VeraDevice { Id = 1, Name = "Thermostat", Room = "Living", Temperature = "23.5" };

        var result = VeraEventMapper.Map(device, DateTimeOffset.UtcNow);

        Assert.Equal(HomeEventType.TemperatureChanged, result.Type);
        Assert.Equal("23.5", result.Value);
    }

    [Fact]
    public void Map_maps_tripped_device_as_motion()
    {
        var device = new VeraDevice { Id = 2, Name = "Motion", Room = "Garage", Tripped = "1" };

        var result = VeraEventMapper.Map(device, DateTimeOffset.UtcNow);

        Assert.Equal(HomeEventType.MotionDetected, result.Type);
        Assert.Equal("true", result.Value);
    }
}
