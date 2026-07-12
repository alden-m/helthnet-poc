using Microsoft.AspNetCore.Hosting;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Persistence lives in a project-relative <c>app_data</c> folder (under the content root), so it travels
/// with the app and persists on Azure App Service — unlike the OS roaming app-data folder, which is
/// ephemeral there. The API key is NOT stored here; it comes from configuration (appsettings.json /
/// Azure App Settings). Directories are created on demand.
/// </summary>
public class AppPaths
{
    public string DataDir { get; }
    public string SettingsFile { get; }
    public string HistoryDir { get; }

    /// <summary>Files uploaded in Settings that are sent to the AI as reference material with every request.</summary>
    public string KnowledgeBaseDir { get; }

    public AppPaths(IWebHostEnvironment env)
    {
        DataDir = Path.Combine(env.ContentRootPath, "app_data");
        SettingsFile = Path.Combine(DataDir, "settings.json");
        HistoryDir = Path.Combine(DataDir, "history");
        KnowledgeBaseDir = Path.Combine(DataDir, "knowledge-base");
    }

    public void EnsureDirs()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(HistoryDir);
        Directory.CreateDirectory(KnowledgeBaseDir);
    }
}
