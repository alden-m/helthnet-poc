using FindMyPath.Poc.Services;
using Microsoft.Extensions.Configuration;

namespace FindMyPath.Poc.Tests.Services;

public class AccessGateServiceTests
{
    [Fact]
    public void MissingConfigurationUsesTheDefaultPin()
    {
        var service = CreateService();

        Assert.True(service.IsValid("n.rayan"));
        Assert.False(service.IsValid("anything-else"));
    }

    [Fact]
    public void BlankConfigurationUsesTheDefaultPin()
    {
        var service = CreateService("   ");

        Assert.True(service.IsValid("n.rayan"));
    }

    [Fact]
    public void ConfiguredPinOverridesTheDefault()
    {
        var service = CreateService("reviewer-2026");

        Assert.True(service.IsValid("reviewer-2026"));
        Assert.False(service.IsValid("n.rayan"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("N.RAYAN")]
    [InlineData("n.rayan ")]
    [InlineData(" n.rayan")]
    public void PinMatchingIsExact(string? candidate)
    {
        var service = CreateService("n.rayan");

        Assert.False(service.IsValid(candidate));
    }

    private static AccessGateService CreateService(string? pin = null)
    {
        var values = new Dictionary<string, string?>();
        if (pin is not null)
        {
            values[AccessGateService.ConfigurationKey] = pin;
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new AccessGateService(configuration);
    }
}
