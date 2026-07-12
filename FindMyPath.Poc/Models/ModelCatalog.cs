namespace FindMyPath.Poc.Models;

/// <summary>An Anthropic model plus its per-million-token pricing (USD), used for cost estimates.</summary>
public record ModelInfo(string Id, string DisplayName, decimal InputPerMTok, decimal OutputPerMTok,
    bool SupportsEffort = true);

/// <summary>Available Anthropic models for the Settings dropdown, with pricing for cost tracking.</summary>
public static class ModelCatalog
{
    public const string DefaultModel = "claude-opus-4-8";

    // Pricing per 1M tokens (input / output), from the Anthropic model catalog.
    public static readonly IReadOnlyList<ModelInfo> Models = new[]
    {
        new ModelInfo("claude-opus-4-8", "Claude Opus 4.8 (recommended)", 5.00m, 25.00m),
        new ModelInfo("claude-opus-4-7", "Claude Opus 4.7", 5.00m, 25.00m),
        // Sonnet 5 introductory pricing is in effect through August 31, 2026.
        new ModelInfo("claude-sonnet-5", "Claude Sonnet 5", 2.00m, 10.00m),
        new ModelInfo("claude-haiku-4-5", "Claude Haiku 4.5", 1.00m, 5.00m, SupportsEffort: false),
        new ModelInfo("claude-fable-5", "Claude Fable 5 (most capable)", 10.00m, 50.00m),
    };

    public static ModelInfo Find(string? id) =>
        Models.FirstOrDefault(m => m.Id == id) ?? Models.First(m => m.Id == DefaultModel);

    public static string DisplayName(string? id) => Find(id).DisplayName;

    /// <summary>Estimated USD cost. Cache reads bill at ~0.1x input; cache writes at ~1.25x input.</summary>
    public static decimal ComputeCost(string? modelId, long inputTokens, long outputTokens,
        long cacheReadTokens = 0, long cacheWriteTokens = 0)
    {
        var m = Find(modelId);
        return inputTokens / 1_000_000m * m.InputPerMTok
             + outputTokens / 1_000_000m * m.OutputPerMTok
             + cacheReadTokens / 1_000_000m * m.InputPerMTok * 0.1m
             + cacheWriteTokens / 1_000_000m * m.InputPerMTok * 1.25m;
    }
}
