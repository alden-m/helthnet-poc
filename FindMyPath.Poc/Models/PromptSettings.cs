namespace FindMyPath.Poc.Models;

/// <summary>Tunable settings persisted to the app-data settings.json. The API key lives here (never in the repo).</summary>
public class PromptSettings
{
    public string Model { get; set; } = ModelCatalog.DefaultModel;

    /// <summary>The editable system instruction (guidance only). The mandatory JSON-output contract is
    /// appended at request time from <see cref="DefaultPrompt.OutputFormatInstruction"/> and never stored here.</summary>
    public string SystemInstruction { get; set; } = "";
}
