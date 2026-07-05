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

    public AppPaths(IWebHostEnvironment env)
    {
        DataDir = Path.Combine(env.ContentRootPath, "app_data");
        SettingsFile = Path.Combine(DataDir, "settings.json");
        HistoryDir = Path.Combine(DataDir, "history");
    }

    public void EnsureDirs()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(HistoryDir);
    }
}
