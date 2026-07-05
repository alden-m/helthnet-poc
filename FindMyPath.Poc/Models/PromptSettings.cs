namespace FindMyPath.Poc.Models;

/// <summary>Tunable settings persisted to the app-data settings.json. The API key lives here (never in the repo).</summary>
public class PromptSettings
{
    public string Model { get; set; } = ModelCatalog.DefaultModel;
    public string SystemInstruction { get; set; } = "";
    public string ReferenceMaterial { get; set; } = "";
}
