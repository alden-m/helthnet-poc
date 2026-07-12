using Anthropic.Helpers;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Tests.Services;

public class RoadmapSchemaTests
{
    [Fact]
    public void StructuredOutputSchemaUsesTheStableCamelCaseRoadmapContract()
    {
        var json = StructuredOutput.ToJsonSchema<RoadmapDto>().ToJsonString();

        Assert.Contains("\"summary\"", json, StringComparison.Ordinal);
        Assert.Contains("\"recommendedPathway\"", json, StringComparison.Ordinal);
        Assert.Contains("\"estimatedTotalTimeline\"", json, StringComparison.Ordinal);
        Assert.Contains("\"estimatedTotalCost\"", json, StringComparison.Ordinal);
        Assert.Contains("\"estimatedTimeline\"", json, StringComparison.Ordinal);
        Assert.Contains("\"estimatedCost\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Summary\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void StructuredOutputFormatCanBeCreatedForEveryCatalogModel()
    {
        foreach (var model in ModelCatalog.Models)
        {
            var format = StructuredOutput.CreateJsonFormat<RoadmapDto>();
            Assert.NotNull(format);
            Assert.False(string.IsNullOrWhiteSpace(model.Id));
        }
    }
}
