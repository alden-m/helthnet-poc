using System.Text.Json;
using FindMyPath.Poc.Models;
using Microsoft.Extensions.Configuration;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Runtime tuning (system instruction, model, reference material) persisted to the project-relative
/// <c>app_data/settings.json</c>. Defaults come from configuration (Anthropic:Model / Anthropic:SystemInstruction
/// / Anthropic:ReferenceMaterial) with built-in fallbacks. The API key is read from configuration only
/// (Anthropic:ApiKey, or the ANTHROPIC_API_KEY env var) — it is never written to app_data. Never crashes on I/O.
/// </summary>
public class PromptSettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly object _lock = new();
    private readonly IConfiguration _config;
    private readonly AppPaths _paths;
    private PromptSettings _settings;

    public PromptSettingsService(IConfiguration config, AppPaths paths)
    {
        _config = config;
        _paths = paths;
        _settings = Load();
    }

    public PromptSettings Current
    {
        get { lock (_lock) { return Clone(_settings); } }
    }

    /// <summary>API key from configuration (Anthropic:ApiKey), falling back to the ANTHROPIC_API_KEY env var.</summary>
    public string? ApiKey
    {
        get
        {
            var fromConfig = _config["Anthropic:ApiKey"];
            if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig;
            return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public void Save(string systemInstruction, string model, string? referenceMaterial)
    {
        lock (_lock)
        {
            _settings.SystemInstruction = string.IsNullOrWhiteSpace(systemInstruction) ? DefaultSystem() : systemInstruction;
            _settings.Model = string.IsNullOrWhiteSpace(model) ? DefaultModel() : model;
            _settings.ReferenceMaterial = referenceMaterial ?? "";
            Persist();
        }
    }

    public string ResetSystemInstruction()
    {
        lock (_lock)
        {
            _settings.SystemInstruction = DefaultSystem();
            Persist();
            return _settings.SystemInstruction;
        }
    }

    private string DefaultModel()
    {
        var m = _config["Anthropic:Model"];
        return string.IsNullOrWhiteSpace(m) ? ModelCatalog.DefaultModel : m;
    }

    private string DefaultSystem()
    {
        var s = _config["Anthropic:SystemInstruction"];
        return string.IsNullOrWhiteSpace(s) ? DefaultPrompt.SystemInstruction : s;
    }

    private string DefaultReference() => _config["Anthropic:ReferenceMaterial"] ?? "";

    private PromptSettings Load()
    {
        try
        {
            _paths.EnsureDirs();
            if (File.Exists(_paths.SettingsFile))
            {
                var s = JsonSerializer.Deserialize<PromptSettings>(File.ReadAllText(_paths.SettingsFile), JsonOpts);
                if (s is not null)
                {
                    if (string.IsNullOrWhiteSpace(s.SystemInstruction)) s.SystemInstruction = DefaultSystem();
                    if (string.IsNullOrWhiteSpace(s.Model)) s.Model = DefaultModel();
                    return s;
                }
            }
        }
        catch
        {
            // fall through to config-seeded defaults
        }
        return new PromptSettings
        {
            Model = DefaultModel(),
            SystemInstruction = DefaultSystem(),
            ReferenceMaterial = DefaultReference(),
        };
    }

    private void Persist()
    {
        try
        {
            _paths.EnsureDirs();
            File.WriteAllText(_paths.SettingsFile, JsonSerializer.Serialize(_settings, JsonOpts));
        }
        catch
        {
            // never crash on save
        }
    }

    private static PromptSettings Clone(PromptSettings s) => new()
    {
        Model = s.Model,
        SystemInstruction = s.SystemInstruction,
        ReferenceMaterial = s.ReferenceMaterial
    };
}
