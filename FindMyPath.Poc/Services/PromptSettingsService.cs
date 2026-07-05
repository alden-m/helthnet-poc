using System.Text.Json;
using FindMyPath.Poc.Models;
using Microsoft.Extensions.Configuration;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Loads and persists tunable settings (system instruction, model, reference material, API key)
/// to the app-data settings.json. Never crashes on read/write; falls back to defaults.
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
    private PromptSettings _settings;

    public PromptSettingsService(IConfiguration config)
    {
        _config = config;
        _settings = Load();
    }

    public PromptSettings Current
    {
        get { lock (_lock) { return Clone(_settings); } }
    }

    /// <summary>API key from the settings file, falling back to the ANTHROPIC_API_KEY environment variable.</summary>
    public string? ApiKey
    {
        get
        {
            // 1) appsettings.json (Anthropic:ApiKey) - the primary place to put the key.
            var fromConfig = _config["Anthropic:ApiKey"];
            if (!string.IsNullOrWhiteSpace(fromConfig)) return fromConfig;

            // 2) app-data settings.json (fallback).
            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(_settings.ApiKey)) return _settings.ApiKey;
            }

            // 3) ANTHROPIC_API_KEY environment variable (fallback).
            return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public void Save(string systemInstruction, string model, string? referenceMaterial)
    {
        lock (_lock)
        {
            _settings.SystemInstruction = string.IsNullOrWhiteSpace(systemInstruction)
                ? DefaultPrompt.SystemInstruction : systemInstruction;
            _settings.Model = string.IsNullOrWhiteSpace(model) ? ModelCatalog.DefaultModel : model;
            _settings.ReferenceMaterial = referenceMaterial ?? "";
            Persist();
        }
    }

    public string ResetSystemInstruction()
    {
        lock (_lock)
        {
            _settings.SystemInstruction = DefaultPrompt.SystemInstruction;
            Persist();
            return _settings.SystemInstruction;
        }
    }

    private PromptSettings Load()
    {
        try
        {
            AppPaths.EnsureDirs();
            if (File.Exists(AppPaths.SettingsFile))
            {
                var s = JsonSerializer.Deserialize<PromptSettings>(File.ReadAllText(AppPaths.SettingsFile), JsonOpts);
                if (s is not null)
                {
                    if (string.IsNullOrWhiteSpace(s.SystemInstruction)) s.SystemInstruction = DefaultPrompt.SystemInstruction;
                    if (string.IsNullOrWhiteSpace(s.Model)) s.Model = ModelCatalog.DefaultModel;
                    return s;
                }
            }
        }
        catch
        {
            // fall through to defaults
        }
        return new PromptSettings
        {
            SystemInstruction = DefaultPrompt.SystemInstruction,
            Model = ModelCatalog.DefaultModel
        };
    }

    private void Persist()
    {
        try
        {
            AppPaths.EnsureDirs();
            File.WriteAllText(AppPaths.SettingsFile, JsonSerializer.Serialize(_settings, JsonOpts));
        }
        catch
        {
            // never crash on save
        }
    }

    private static PromptSettings Clone(PromptSettings s) => new()
    {
        ApiKey = s.ApiKey,
        Model = s.Model,
        SystemInstruction = s.SystemInstruction,
        ReferenceMaterial = s.ReferenceMaterial
    };
}
