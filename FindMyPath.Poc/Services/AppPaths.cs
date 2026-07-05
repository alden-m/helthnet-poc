namespace FindMyPath.Poc.Services;

/// <summary>
/// Persistence lives under the OS app-data folder (NOT the app directory) so redeploys
/// never wipe settings or history, and the API key never lands in the repo.
/// </summary>
public static class AppPaths
{
    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FindMyPath");

    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
    public static string HistoryDir => Path.Combine(DataDir, "history");

    public static void EnsureDirs()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(HistoryDir);
    }
}
