using System.Text.Json;
using FindMyPath.Poc.Models;

namespace FindMyPath.Poc.Services;

/// <summary>Persists and reads per-submission JSON snapshots under the app-data history folder.</summary>
public class HistoryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string Save(SubmissionRecord record)
    {
        AppPaths.EnsureDirs();
        var path = Path.Combine(AppPaths.HistoryDir, $"{record.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(record, JsonOpts));
        return path;
    }

    public List<SubmissionRecord> List()
    {
        AppPaths.EnsureDirs();
        var list = new List<SubmissionRecord>();
        foreach (var file in Directory.GetFiles(AppPaths.HistoryDir, "*.json"))
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
        return list.OrderByDescending(r => r.TimestampUtc).ToList();
    }

    public SubmissionRecord? Get(string id)
    {
        var path = Path.Combine(AppPaths.HistoryDir, $"{id}.json");
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<SubmissionRecord>(File.ReadAllText(path), JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}
