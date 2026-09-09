using System.Collections.Generic;

namespace Ownly.Core.Tweaks;

public enum TweakKind
{
    /// <summary>Sets one or more registry values. HKCU is applied in-process; HKLM needs admin.</summary>
    Registry,
    /// <summary>Runs a PowerShell snippet (flush DNS, toggle a service, powercfg, sfc, …).</summary>
    Action
}

/// <summary>
/// One registry value to set. <see cref="Type"/> is "DWord", "String" or "Delete".
/// </summary>
public sealed record RegValue(string Hive, string Path, string Name, string Type, long Dword = 0, string Text = "");

public sealed record Tweak(
    string Id,
    string Section,
    string Group,
    string Title,
    string Description,
    string Risk,
    TweakKind Kind,
    bool NeedsAdmin,
    bool Reversible,
    string? Note = null,
    IReadOnlyList<RegValue>? Registry = null,
    string? ActionApply = null,
    string? ActionRevert = null,
    bool ActionWait = true,
    bool ActionShowWindow = true,
    int ActionTimeoutSeconds = 90)
{
    public string ChangeId => "tweak:" + Id;
}
