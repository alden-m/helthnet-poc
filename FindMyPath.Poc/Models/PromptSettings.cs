using FindMyPath.Poc.Services;

namespace FindMyPath.Poc.Models;

/// <summary>Tunable, non-secret generation settings persisted to app-data/settings.json.</summary>
public class PromptSettings
{
    public string Model { get; set; } = ModelCatalog.DefaultModel;

    /// <summary>Whether files in the HealthNet knowledge base are attached to each generation.</summary>
    public bool IncludeKnowledgeBase { get; set; }

    /// <summary>Anthropic output effort: low, medium, high, or max (ignored by models that do not support it).</summary>
    public string Effort { get; set; } = GenerationTuningCatalog.DefaultEffort;

    /// <summary>Hard cap on generated output, including any model reasoning tokens.</summary>
    public int MaxOutputTokens { get; set; } = GenerationTuningCatalog.DefaultMaxOutputTokens;

    /// <summary>The editable system instruction (guidance only). The mandatory JSON-output contract is
    /// appended at request time from <see cref="DefaultPrompt.OutputFormatInstruction"/> and never stored here.</summary>
    public string SystemInstruction { get; set; } = "";

    /// <summary>Editable roadmap length and organization guidance. Blank values use
    /// <see cref="DefaultPrompt.OutputStyleInstruction"/> when a request is built.</summary>
    public string OutputStyleInstruction { get; set; } = DefaultPrompt.OutputStyleInstruction;
}
