using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Ownly.Core.Safety;

namespace Ownly.Core.Tweaks;

/// <summary>
/// Applies and reverts <see cref="Tweak"/>s. HKCU registry changes happen in-process; HKLM changes
/// and actions run through a PowerShell window with a Windows permission prompt. Every run is written
/// to the <see cref="ActivityLog"/>, and reversible changes also get a <see cref="ChangeRecord"/>.
/// </summary>
public sealed class TweakRunner
{
    private readonly ChangeLogService _changes = new();
    private readonly ActivityLog _activity = new();

    public bool IsApplied(Tweak tweak)
    {
        if (tweak.Kind == TweakKind.Registry && tweak.Registry is not null)
        {
            return tweak.Registry.All(MatchesTarget);
        }

        return _changes.Read().Any(c => c.FeatureId == tweak.ChangeId && !c.IsRestored);
    }

    public ActionResult Apply(Tweak tweak)
    {
        return tweak.Kind == TweakKind.Registry ? ApplyRegistry(tweak) : ApplyAction(tweak);
    }

    // ---------- registry ----------

    private ActionResult ApplyRegistry(Tweak tweak)
    {
        var ops = tweak.Registry!;
        var before = ops.Select(Capture).ToList();
        var needsElevation = ops.Any(o => o.Hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase));

        try
        {
            string output;
            if (needsElevation)
            {
                var result = ElevatedRunner.Run(BuildApplyScript(ops), elevated: true, showWindow: true, wait: true);
                if (result.Cancelled)
                {
                    return new ActionResult(false, result.Output);
                }
                if (!result.Success)
                {
                    _activity.Append(tweak.Title, tweak.Description, "Registry (admin)", true, false, result.Output);
                    return new ActionResult(false, "Windows reported a problem applying this. See Activity for details.");
                }
                output = result.Output;
            }
            else
            {
                foreach (var op in ops)
                {
                    WriteCurrentUser(op);
                }
                output = "Applied " + string.Join(", ", ops.Select(o => o.Name));
            }

            var payload = JsonSerializer.Serialize(new RevertPayload("reg", before, null, needsElevation));
            _changes.Record(new ChangeRecord(
                Guid.NewGuid().ToString("N"), tweak.ChangeId, tweak.Title, SectionName(tweak.Section),
                tweak.Risk, DateTimeOffset.Now, payload, "applied", tweak.Reversible,
                tweak.Description + (tweak.Note is null ? "" : "  " + tweak.Note)));
            _activity.Append(tweak.Title, tweak.Description, needsElevation ? "Registry (admin)" : "Registry", needsElevation, true, output);
            return new ActionResult(true, tweak.Note ?? "Applied. You can undo this from Changes.");
        }
        catch (Exception ex)
        {
            _activity.Append(tweak.Title, tweak.Description, "Registry", needsElevation, false, ex.Message);
            return new ActionResult(false, "Ownly could not apply this: " + ex.Message);
        }
    }

    // ---------- action ----------

    private ActionResult ApplyAction(Tweak tweak)
    {
        var result = ElevatedRunner.Run(tweak.ActionApply!, tweak.NeedsAdmin, tweak.ActionShowWindow, tweak.ActionWait);
        _activity.Append(tweak.Title, tweak.Description, tweak.NeedsAdmin ? "Command (admin)" : "Command", tweak.NeedsAdmin, result.Success, result.Output);

        if (result.Cancelled)
        {
            return new ActionResult(false, result.Output);
        }

        if (result.Success && tweak.Reversible && tweak.ActionRevert is not null)
        {
            var payload = JsonSerializer.Serialize(new RevertPayload("action", null, tweak.ActionRevert, tweak.NeedsAdmin));
            _changes.Record(new ChangeRecord(
                Guid.NewGuid().ToString("N"), tweak.ChangeId, tweak.Title, SectionName(tweak.Section),
                tweak.Risk, DateTimeOffset.Now, payload, "applied", true, tweak.Description));
        }

        if (!tweak.ActionWait)
        {
            return new ActionResult(true, "Started in a new window — watch it there.");
        }
        return result.Success
            ? new ActionResult(true, string.IsNullOrWhiteSpace(result.Output) ? "Done." : Shorten(result.Output))
            : new ActionResult(false, "That did not complete. See Activity for the output.");
    }

    // ---------- revert ----------

    public ActionResult RevertByTweak(Tweak tweak)
    {
        var change = _changes.Read()
            .Where(c => c.FeatureId == tweak.ChangeId && !c.IsRestored)
            .OrderByDescending(c => c.AppliedAt)
            .FirstOrDefault();
        return change is null
            ? new ActionResult(false, "Ownly has no saved previous value for this one, so it will not change it back automatically.")
            : Revert(change);
    }

    public ActionResult Revert(ChangeRecord change)
    {
        RevertPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<RevertPayload>(change.BeforeState);
        }
        catch
        {
            return new ActionResult(false, "Ownly cannot read what this change was, so it will not try to undo it.");
        }
        if (payload is null)
        {
            return new ActionResult(false, "There is nothing recorded to restore.");
        }

        try
        {
            if (payload.Mode == "action" && payload.Script is not null)
            {
                var res = ElevatedRunner.Run(payload.Script, payload.Admin, showWindow: true, wait: true);
                _activity.Append(change.FeatureName + " — undo", "Restore previous state", payload.Admin ? "Command (admin)" : "Command", payload.Admin, res.Success, res.Output);
                if (res.Cancelled) return new ActionResult(false, res.Output);
                if (!res.Success) return new ActionResult(false, "The undo command did not complete. See Activity.");
            }
            else if (payload.Mode == "reg" && payload.Values is not null)
            {
                if (payload.Admin)
                {
                    var res = ElevatedRunner.Run(BuildRevertScript(payload.Values), elevated: true, showWindow: true, wait: true);
                    _activity.Append(change.FeatureName + " — undo", "Restore previous registry values", "Registry (admin)", true, res.Success, res.Output);
                    if (res.Cancelled) return new ActionResult(false, res.Output);
                    if (!res.Success) return new ActionResult(false, "Windows reported a problem undoing this. See Activity.");
                }
                else
                {
                    foreach (var v in payload.Values)
                    {
                        RestoreCurrentUser(v);
                    }
                    _activity.Append(change.FeatureName + " — undo", "Restore previous registry values", "Registry", false, true, "Restored " + string.Join(", ", payload.Values.Select(v => v.Name)));
                }
            }
            else
            {
                return new ActionResult(false, "This change is not one Ownly can undo automatically.");
            }

            _changes.MarkRestored(change.Id);
            return new ActionResult(true, "Restored the previous setting.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, "Ownly could not undo this: " + ex.Message);
        }
    }

    // ---------- registry helpers ----------

    private static bool MatchesTarget(RegValue op)
    {
        var current = ReadValue(op.Hive, op.Path, op.Name);
        if (op.Type == "Delete")
        {
            return current is null;
        }
        if (current is null)
        {
            return false;
        }
        return op.Type == "String"
            ? string.Equals(current.ToString(), op.Text, StringComparison.OrdinalIgnoreCase)
            : current is int i && i == op.Dword || current is long l && l == op.Dword
              || int.TryParse(current.ToString(), out var pi) && pi == op.Dword;
    }

    private static CapturedValue Capture(RegValue op)
    {
        var current = ReadValue(op.Hive, op.Path, op.Name);
        return current is null
            ? new CapturedValue(op.Hive, op.Path, op.Name, false, "None", 0, "")
            : current is string s
                ? new CapturedValue(op.Hive, op.Path, op.Name, true, "String", 0, s)
                : new CapturedValue(op.Hive, op.Path, op.Name, true, "DWord", Convert.ToInt64(current), "");
    }

    private static object? ReadValue(string hive, string path, string name)
    {
        try
        {
            using var root = hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase)
                ? Registry.LocalMachine : Registry.CurrentUser;
            using var key = root.OpenSubKey(path, writable: false);
            return key?.GetValue(name);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteCurrentUser(RegValue op)
    {
        if (op.Type == "Delete")
        {
            using var key = Registry.CurrentUser.OpenSubKey(op.Path, writable: true);
            key?.DeleteValue(op.Name, throwOnMissingValue: false);
            return;
        }
        using var writeKey = Registry.CurrentUser.CreateSubKey(op.Path, writable: true)
            ?? throw new InvalidOperationException("Could not open " + op.Path);
        if (op.Type == "String")
        {
            writeKey.SetValue(op.Name, op.Text, RegistryValueKind.String);
        }
        else
        {
            writeKey.SetValue(op.Name, (int)op.Dword, RegistryValueKind.DWord);
        }
    }

    private static void RestoreCurrentUser(CapturedValue v)
    {
        if (!v.Existed)
        {
            using var key = Registry.CurrentUser.OpenSubKey(v.Path, writable: true);
            key?.DeleteValue(v.Name, throwOnMissingValue: false);
            return;
        }
        using var writeKey = Registry.CurrentUser.CreateSubKey(v.Path, writable: true)!;
        if (v.Type == "String")
        {
            writeKey.SetValue(v.Name, v.Text, RegistryValueKind.String);
        }
        else
        {
            writeKey.SetValue(v.Name, (int)v.Dword, RegistryValueKind.DWord);
        }
    }

    private static string PsHive(string hive) => hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase) ? "HKLM:" : "HKCU:";

    private static string BuildApplyScript(IEnumerable<RegValue> ops)
    {
        var sb = new StringBuilder();
        foreach (var op in ops)
        {
            var full = PsHive(op.Hive) + "\\" + op.Path;
            if (op.Type == "Delete")
            {
                sb.AppendLine($"Remove-ItemProperty -Path '{full}' -Name '{op.Name}' -ErrorAction SilentlyContinue");
                continue;
            }
            sb.AppendLine($"if (-not (Test-Path '{full}')) {{ New-Item -Path '{full}' -Force | Out-Null }}");
            sb.AppendLine(op.Type == "String"
                ? $"New-ItemProperty -Path '{full}' -Name '{op.Name}' -PropertyType String -Value '{op.Text}' -Force | Out-Null"
                : $"New-ItemProperty -Path '{full}' -Name '{op.Name}' -PropertyType DWord -Value {op.Dword} -Force | Out-Null");
            sb.AppendLine($"Write-Output \"Set {op.Name}\"");
        }
        return sb.ToString();
    }

    private static string BuildRevertScript(IEnumerable<CapturedValue> values)
    {
        var sb = new StringBuilder();
        foreach (var v in values)
        {
            var full = PsHive(v.Hive) + "\\" + v.Path;
            if (!v.Existed)
            {
                sb.AppendLine($"Remove-ItemProperty -Path '{full}' -Name '{v.Name}' -ErrorAction SilentlyContinue");
                sb.AppendLine($"Write-Output \"Removed {v.Name}\"");
                continue;
            }
            sb.AppendLine($"if (-not (Test-Path '{full}')) {{ New-Item -Path '{full}' -Force | Out-Null }}");
            sb.AppendLine(v.Type == "String"
                ? $"New-ItemProperty -Path '{full}' -Name '{v.Name}' -PropertyType String -Value '{v.Text}' -Force | Out-Null"
                : $"New-ItemProperty -Path '{full}' -Name '{v.Name}' -PropertyType DWord -Value {v.Dword} -Force | Out-Null");
            sb.AppendLine($"Write-Output \"Restored {v.Name}\"");
        }
        return sb.ToString();
    }

    private static string SectionName(string section) =>
        section.Length == 0 ? section : char.ToUpperInvariant(section[0]) + section[1..];

    private static string Shorten(string s) => s.Length <= 200 ? s : s[..200].Trim() + " …";

    // ---------- payload types ----------

    public sealed record CapturedValue(string Hive, string Path, string Name, bool Existed, string Type, long Dword, string Text);

    public sealed record RevertPayload(string Mode, List<CapturedValue>? Values, string? Script, bool Admin);
}
