using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Tests.Models;

public class ModelCatalogTests
{
    [Fact]
    public void CatalogAndDefaultModelMatchTheApprovedPricingTable()
    {
        ModelInfo[] expected =
        [
            new("claude-opus-4-8", "Claude Opus 4.8 (recommended)", 5.00m, 25.00m),
            new("claude-opus-4-7", "Claude Opus 4.7", 5.00m, 25.00m),
            new("claude-sonnet-5", "Claude Sonnet 5", 2.00m, 10.00m),
            new("claude-haiku-4-5", "Claude Haiku 4.5", 1.00m, 5.00m, SupportsEffort: false),
            new("claude-fable-5", "Claude Fable 5 (most capable)", 10.00m, 50.00m),
        ];

        Assert.Equal("claude-opus-4-8", ModelCatalog.DefaultModel);
        Assert.Equal(expected.Count(), ModelCatalog.Models.Count);
        Assert.Equal(expected, ModelCatalog.Models);
    }

    [Fact]
    public void FindAndDisplayNameFallBackToTheDefaultForUnknownOrNullIds()
    {
        var expected = ModelCatalog.Models.Single(m => m.Id == ModelCatalog.DefaultModel);

        Assert.Equal(expected, ModelCatalog.Find(null));
        Assert.Equal(expected, ModelCatalog.Find("not-a-real-model"));
        Assert.Equal(expected.DisplayName, ModelCatalog.DisplayName(null));
        Assert.Equal(expected.DisplayName, ModelCatalog.DisplayName("not-a-real-model"));
    }

    [Theory]
    [InlineData("claude-opus-4-8", 5, 25)]
    [InlineData("claude-opus-4-7", 5, 25)]
    [InlineData("claude-sonnet-5", 2, 10)]
    [InlineData("claude-haiku-4-5", 1, 5)]
    [InlineData("claude-fable-5", 10, 50)]
    public void ComputeCostUsesPerMillionInputAndOutputPrices(
        string model,
        int expectedInputCost,
        int expectedOutputCost)
    {
        Assert.Equal(expectedInputCost, ModelCatalog.ComputeCost(model, 1_000_000, 0));
        Assert.Equal(expectedOutputCost, ModelCatalog.ComputeCost(model, 0, 1_000_000));
    }

    [Fact]
    public void ComputeCostIncludesCacheReadAndWriteMultipliers()
    {
        var cost = ModelCatalog.ComputeCost(
            "claude-opus-4-8",
            inputTokens: 1_000_000,
            outputTokens: 1_000_000,
            cacheReadTokens: 1_000_000,
            cacheWriteTokens: 1_000_000);

        Assert.Equal(36.75m, cost);
    }

    [Fact]
    public void ComputeCostPreservesFractionalPrecisionAndUsesDefaultForUnknownModel()
    {
        var expected = ModelCatalog.ComputeCost(ModelCatalog.DefaultModel, 1234, 5678, 90, 12);
        var fallback = ModelCatalog.ComputeCost("unknown", 1234, 5678, 90, 12);

        Assert.Equal(0.148240m, expected);
        Assert.Equal(expected, fallback);
        Assert.Equal(0m, ModelCatalog.ComputeCost(ModelCatalog.DefaultModel, 0, 0));
    }
}
