using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Ownly.Core.Safety;

public sealed class ChangeLogService
{
    private readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ownly",
        "changes.json");

    public IReadOnlyList<ChangeRecord> Read()
    {
        try
        {
            if (!File.Exists(_logPath))
            {
                return Array.Empty<ChangeRecord>();
            }

            return JsonSerializer.Deserialize<List<ChangeRecord>>(File.ReadAllText(_logPath))
                ?? new List<ChangeRecord>();
        }
        catch
        {
            return Array.Empty<ChangeRecord>();
        }
    }

    public void Record(ChangeRecord change)
    {
        var directory = Path.GetDirectoryName(_logPath)!;
        Directory.CreateDirectory(directory);
        var changes = Read().Where(existing => existing.Id != change.Id).ToList();
        changes.Add(change);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_logPath, JsonSerializer.Serialize(changes, options));
    }

    public void MarkRestored(string changeId)
    {
        var changes = Read()
            .Select(change => change.Id == changeId ? change with { IsRestored = true } : change)
            .ToList();
        var directory = Path.GetDirectoryName(_logPath)!;
        Directory.CreateDirectory(directory);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_logPath, JsonSerializer.Serialize(changes, options));
    }
}
