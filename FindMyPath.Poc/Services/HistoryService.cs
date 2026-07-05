using System.Text.Json;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Persists and reads per-submission JSON snapshots under the project-relative <c>app_data/history</c> folder.
/// Never crashes on I/O (best-effort persistence).
/// </summary>
public class HistoryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly AppPaths _paths;

    public HistoryService(AppPaths paths) => _paths = paths;

    public void Save(SubmissionRecord record)
    {
        try
        {
            _paths.EnsureDirs();
            File.WriteAllText(Path.Combine(_paths.HistoryDir, $"{record.Id}.json"),
                JsonSerializer.Serialize(record, JsonOpts));
        }
        catch
        {
            // history is best-effort; never block a generation
        }
    }

    public List<SubmissionRecord> List()
    {
        var list = new List<SubmissionRecord>();
        try
        {
            _paths.EnsureDirs();
            foreach (var file in Directory.GetFiles(_paths.HistoryDir, "*.json"))
            {
                try
                {
                    var rec = JsonSerializer.Deserialize<SubmissionRecord>(File.ReadAllText(file), JsonOpts);
                    if (rec is not null) list.Add(rec);
                }
                catch
                {
                    // skip unreadable snapshots
                }
            }
        }
        catch
        {
            // return whatever we have
        }
        return list.OrderByDescending(r => r.TimestampUtc).ToList();
    }

    public SubmissionRecord? Get(string id)
    {
        try
        {
            var path = Path.Combine(_paths.HistoryDir, $"{id}.json");
            if (File.Exists(path))
                return JsonSerializer.Deserialize<SubmissionRecord>(File.ReadAllText(path), JsonOpts);
        }
        catch
        {
            // ignore
        }
        return null;
    }
}
