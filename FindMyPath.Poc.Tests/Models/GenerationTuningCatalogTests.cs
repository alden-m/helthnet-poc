using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Tests.Models;

public class GenerationTuningCatalogTests
{
    [Fact]
    public void DefaultsAndExposedChoicesAreStableAndDemoSafe()
    {
        Assert.Equal("high", GenerationTuningCatalog.DefaultEffort);
        Assert.Equal(8192, GenerationTuningCatalog.DefaultMaxOutputTokens);
        Assert.Equal(
            ["low", "medium", "high", "max"],
            GenerationTuningCatalog.Efforts.Select(e => e.Value));
        Assert.Equal(
            [4096, 8192, 16000],
            GenerationTuningCatalog.TokenBudgets.Select(t => t.Value));
        Assert.All(GenerationTuningCatalog.Efforts, option =>
        {
            Assert.False(string.IsNullOrWhiteSpace(option.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(option.Description));
        });
        Assert.All(GenerationTuningCatalog.TokenBudgets, option =>
        {
            Assert.False(string.IsNullOrWhiteSpace(option.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(option.Description));
        });
    }

    [Theory]
    [InlineData("low", "low")]
    [InlineData("MEDIUM", "medium")]
    [InlineData("High", "high")]
    [InlineData("max", "max")]
    [InlineData("unsupported", "high")]
    [InlineData(null, "high")]
    public void NormalizeEffortAcceptsKnownValuesCaseInsensitivelyAndDefaultsEverythingElse(
        string? input,
        string expected)
    {
        Assert.Equal(expected, GenerationTuningCatalog.NormalizeEffort(input));
    }

    [Theory]
    [InlineData(4096, 4096)]
    [InlineData(8192, 8192)]
    [InlineData(16000, 16000)]
    [InlineData(0, 8192)]
    [InlineData(-1, 8192)]
    [InlineData(999999, 8192)]
    public void NormalizeMaxOutputTokensAcceptsOnlyExposedBudgets(int input, int expected)
    {
        Assert.Equal(expected, GenerationTuningCatalog.NormalizeMaxOutputTokens(input));
    }
}
