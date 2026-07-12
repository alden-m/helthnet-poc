namespace FindMyPath.Poc.Models;

public record EffortOption(string Value, string DisplayName, string Description);
public record TokenBudgetOption(int Value, string DisplayName, string Description);

/// <summary>Small, safe set of generation controls exposed in Prompt Settings.</summary>
public static class GenerationTuningCatalog
{
    public const string DefaultEffort = "high";
    public const int DefaultMaxOutputTokens = 8192;

    public static readonly IReadOnlyList<EffortOption> Efforts =
    [
        new("low", "Low - fastest", "Faster and less expensive; best for quick comparisons."),
        new("medium", "Medium - balanced", "Balances response quality, latency, and cost."),
        new("high", "High - thorough (recommended)", "Prioritises a careful, complete pathway."),
        new("max", "Max - deepest", "Highest effort; slower and potentially more expensive."),
    ];

    public static readonly IReadOnlyList<TokenBudgetOption> TokenBudgets =
    [
        new(4096, "4,096 - concise", "A shorter client-ready roadmap."),
        new(8192, "8,192 - balanced (recommended)", "Enough room for a useful, detailed roadmap."),
        new(16000, "16,000 - detailed", "More room for complex cases and longer reference material."),
    ];

    public static string NormalizeEffort(string? value) =>
        Efforts.Any(e => string.Equals(e.Value, value, StringComparison.OrdinalIgnoreCase))
            ? value!.ToLowerInvariant()
            : DefaultEffort;

    public static int NormalizeMaxOutputTokens(int value) =>
        TokenBudgets.Any(t => t.Value == value) ? value : DefaultMaxOutputTokens;
}
