using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Ownly.Core.Tweaks;

public sealed record ActivityEntry(
    string Id,
    DateTimeOffset At,
    string Title,
    string Detail,
    string Method,
    bool Elevated,
    bool Success,
    string Output);

/// <summary>
/// A plain, append-only record of everything Ownly has done on this PC — what ran, whether it
/// needed administrator rights, and any output it produced. Shown in the Activity section.
/// </summary>
public sealed class ActivityLog
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ownly", "activity.json");

    public IReadOnlyList<ActivityEntry> Read()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<List<ActivityEntry>>(File.ReadAllText(_path)) ?? new()
                : new List<ActivityEntry>();
        }
        catch
        {
            return Array.Empty<ActivityEntry>();
        }
    }

    public void Append(string title, string detail, string method, bool elevated, bool success, string output)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var list = Read().ToList();
            list.Add(new ActivityEntry(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.Now,
                title,
                detail,
                method,
                elevated,
                success,
                Trim(output)));
            if (list.Count > 500)
            {
                list = list.Skip(list.Count - 500).ToList();
            }
            File.WriteAllText(_path, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Logging is best-effort.
        }
    }

    public void Clear()
    {
        try { File.Delete(_path); } catch { }
    }

    private static string Trim(string s) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length > 4000 ? s[..4000] + "\n…(truncated)" : s);
}
