using System;
using System.IO;
using System.Text.Json;

namespace Ownly.Core.App;

/// <summary>
/// Records whether this user has read and accepted the Ownly disclaimer and terms.
/// Acceptance is stored per Windows user under LocalAppData and is tied to a terms version,
/// so a future change to the terms can require a fresh acceptance.
/// </summary>
public sealed class AcceptanceService
{
    public const string CurrentTermsVersion = "2026-09-09";

    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ownly",
        "acceptance.json");

    public bool HasAcceptedCurrent()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return false;
            }

            var record = JsonSerializer.Deserialize<AcceptanceRecord>(File.ReadAllText(_path));
            return record is not null
                && record.Accepted
                && string.Equals(record.TermsVersion, CurrentTermsVersion, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public void RecordAcceptance()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var record = new AcceptanceRecord(
                true,
                CurrentTermsVersion,
                DateTimeOffset.Now,
                Environment.UserName,
                Environment.MachineName);
            File.WriteAllText(_path, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // If the record cannot be written the user will simply be asked again next launch.
        }
    }

    private sealed record AcceptanceRecord(
        bool Accepted,
        string TermsVersion,
        DateTimeOffset AcceptedAt,
        string WindowsUser,
        string Device);
}
