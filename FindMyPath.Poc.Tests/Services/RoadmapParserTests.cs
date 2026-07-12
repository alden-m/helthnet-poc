using FindMyPath.Poc.Services;

namespace FindMyPath.Poc.Tests.Services;

public class RoadmapParserTests
{
    [Theory]
    [InlineData("```json\n{\"summary\":\"Fenced JSON\"}\n```")]
    [InlineData("```\n{\"summary\":\"Fenced JSON\"}\n```")]
    [InlineData("{\"summary\":\"Fenced JSON\"}")]
    [InlineData("Here is the roadmap: {\"summary\":\"Fenced JSON\"} Hope this helps.")]
    public void TryParseAcceptsFencedPlainAndProseWrappedJson(string response)
    {
        var result = RoadmapParser.TryParse(response);

        Assert.NotNull(result);
        Assert.Equal("Fenced JSON", result.Summary);
    }

    [Fact]
    public void TryParseToleratesCommentsTrailingCommasCaseAndScalarStringDeviations()
    {
        const string response = """
            {
              // Models occasionally add a comment even when asked for strict JSON.
              "SUMMARY": true,
              "recommendedPathway": 42,
              "estimatedTotalTimeline": { "unexpected": "object" },
              "estimatedTotalCost": 4500,
              "phases": [
                {
                  "title": "Credential assessment",
                  "description": false,
                  "steps": [
                    {
                      "title": "Open an account",
                      "description": ["unexpected", "array"],
                      "estimatedTimeline": 6,
                      "estimatedCost": null,
                    },
                  ],
                },
              ],
              "notes": null,
            }
            """;

        var result = RoadmapParser.TryParse(response);

        Assert.NotNull(result);
        Assert.Equal("true", result.Summary);
        Assert.Equal("42", result.RecommendedPathway);
        Assert.Equal("", result.EstimatedTotalTimeline);
        Assert.Equal("4500", result.EstimatedTotalCost);
        Assert.Empty(result.Notes);

        var phase = Assert.Single(result.Phases);
        Assert.Equal("Credential assessment", phase.Title);
        Assert.Equal("false", phase.Description);

        var step = Assert.Single(phase.Steps);
        Assert.Equal("Open an account", step.Title);
        Assert.Equal("", step.Description);
        Assert.Equal("6", step.EstimatedTimeline);
        Assert.Equal("", step.EstimatedCost);
    }

    [Fact]
    public void TryParseNormalizesExplicitlyNullCollections()
    {
        var nullTopLevel = RoadmapParser.TryParse(
            """{"summary":"Valid","phases":null,"notes":null}""");
        var nullSteps = RoadmapParser.TryParse(
            """{"summary":"Valid","phases":[{"title":"Phase","steps":null}],"notes":[]}""");

        Assert.NotNull(nullTopLevel);
        Assert.NotNull(nullTopLevel.Phases);
        Assert.Empty(nullTopLevel.Phases);
        Assert.NotNull(nullTopLevel.Notes);
        Assert.Empty(nullTopLevel.Notes);

        Assert.NotNull(nullSteps);
        var phase = Assert.Single(nullSteps.Phases);
        Assert.NotNull(phase.Steps);
        Assert.Empty(phase.Steps);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("There is no JSON here.")]
    [InlineData("{not-json}")]
    [InlineData("{}")]
    [InlineData("{\"summary\":null,\"recommendedPathway\":null,\"phases\":[],\"notes\":[]}")]
    [InlineData("[\"valid JSON, wrong shape\"]")]
    public void TryParseReturnsNullForMissingMalformedOrContentlessRoadmaps(string? response)
    {
        Assert.Null(RoadmapParser.TryParse(response!));
    }
}
