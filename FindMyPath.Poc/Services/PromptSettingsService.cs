using System.Text.Json;
using FindMyPath.Poc.Models;
using Microsoft.Extensions.Configuration;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Runtime tuning (system instruction, model, reference material) persisted to the project-relative
/// <c>app_data/settings.json</c>. Defaults come from code — <see cref="DefaultPrompt.SystemInstruction"/> and
/// <see cref="ModelCatalog.DefaultModel"/> — not from appsettings.json. The only thing read from configuration
/// is the API key (Anthropic:ApiKey, or the ANTHROPIC_API_KEY env var); it is never written to app_data.
/// Never crashes on I/O.
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

    public void Save(string systemInstruction, string model)
    {
        lock (_lock)
        {
            _settings.SystemInstruction = string.IsNullOrWhiteSpace(systemInstruction)
                ? DefaultSystem()
                : StripMandatoryFormat(systemInstruction);
            _settings.Model = string.IsNullOrWhiteSpace(model) ? DefaultModel() : model;
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

    // Defaults are code, not configuration: appsettings.json holds only the API key.
    private static string DefaultModel() => ModelCatalog.DefaultModel;

    private static string DefaultSystem() => DefaultPrompt.SystemInstruction;

    /// <summary>
    /// Removes the mandatory JSON-output contract from an instruction if a legacy settings.json still
    /// carries it inline, so it never resurfaces in the editable Settings textarea. The contract is
    /// re-appended at request time by <see cref="RoadmapService"/>.
    /// </summary>
    private static string StripMandatoryFormat(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var idx = s.IndexOf("Output format:", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && s.IndexOf("single fenced JSON", StringComparison.OrdinalIgnoreCase) > idx)
            return s[..idx].TrimEnd();
        return s;
    }

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
                    s.SystemInstruction = string.IsNullOrWhiteSpace(s.SystemInstruction)
                        ? DefaultSystem()
                        : StripMandatoryFormat(s.SystemInstruction);
                    if (string.IsNullOrWhiteSpace(s.Model)) s.Model = DefaultModel();
                    return s;
                }
            }
        }
        catch
        {
            // fall through to code defaults
        }
        return new PromptSettings
        {
            Model = DefaultModel(),
            SystemInstruction = DefaultSystem(),
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
    };
}
