using System;
using System.Collections.Generic;

namespace Ownly.Core.Features;

public enum SectionItemKind
{
    /// <summary>An on/off per-user Windows preference. Toggling calls SectionActions.Apply / Revert.</summary>
    Toggle,
    /// <summary>A one-off action (e.g. move temp files to backup).</summary>
    Action,
    /// <summary>Opens another Ownly page.</summary>
    Navigate,
    /// <summary>Information only; no control.</summary>
    Info
}

public sealed record SectionItem(
    string Id,
    string Title,
    string Description,
    string Glyph,
    string Risk,
    SectionItemKind Kind,
    string? ActionLabel = null,
    string? NavigateTarget = null);

public sealed record SectionGroup(string Heading, string? Caption, IReadOnlyList<SectionItem> Items);

public sealed record SectionContent(
    string Eyebrow,
    string Title,
    string Description,
    bool ShowScanner,
    bool ShowSystemSnapshot,
    IReadOnlyList<SectionGroup> Groups);

/// <summary>
/// Describes what each navigation section shows. Everything listed here is either read-only or a
/// reversible per-user change handled by <see cref="SectionActions"/>.
/// </summary>
public static class SectionCatalog
{
    public static SectionContent Get(string section) => section.ToLowerInvariant() switch
    {
        "clean" => new SectionContent(
            "CLEAN", "Clean your PC.",
            "Reversible cleanup and a read-only look at optional software. Nothing is removed without you choosing it.",
            ShowScanner: true, ShowSystemSnapshot: false,
            new[]
            {
                new SectionGroup("Reversible cleanup", "Each toggle is a per-user setting you can switch back from the Changes section.", new[]
                {
                    T("clean.windows-suggestions", "Limit Windows suggestions", "Turn off tips, recommendations, and promoted apps in Start, Settings, and the lock screen.", "", "Low"),
                    T("clean.background-apps", "Limit background apps", "Stop apps you are not using from running in the background for this user.", "", "Moderate"),
                    new SectionItem("clean.temp", "Move temporary files to backup", "Run a scan first, then move eligible temp files into Ownly's private backup instead of deleting them.", "", "Low", SectionItemKind.Action, ActionLabel: "SCAN & CLEAN"),
                }),
                new SectionGroup("Installed software", "Not reversible from Ownly — it runs each program’s own uninstaller.", new[]
                {
                    new SectionItem("clean.uninstall", "Uninstall programs", "See everything installed and remove what you don’t want. Checks for leftovers afterward.", "", "Moderate", SectionItemKind.Navigate, NavigateTarget: "uninstall"),
                }),
            }),

        "customize" => new SectionContent(
            "CUSTOMIZE", "Make Windows yours.",
            "Appearance and Explorer preferences for your user account. Every switch is reversible.",
            false, false,
            new[]
            {
                new SectionGroup("Appearance", null, new[]
                {
                    T("customize.theme", "Dark mode", "Use the dark theme for Windows apps and system surfaces.", "", "Low"),
                    T("customize.compact-menu", "Classic right-click menu", "Restore the full Windows 10 style context menu without the extra 'Show more options' click.", "", "Low"),
                }),
                new SectionGroup("Taskbar & File Explorer", null, new[]
                {
                    T("customize.taskbar", "Left-align the taskbar", "Move taskbar buttons to the left where this Windows version supports it.", "", "Low"),
                    T("customize.file-extensions", "Show file extensions", "Always show the extension (.pdf, .exe, …) for every file in File Explorer.", "", "Low"),
                    T("customize.hidden-files", "Show hidden files", "Show hidden items in File Explorer. Protected system files stay controlled separately.", "", "Low"),
                    T("customize.explorer-home", "Open Explorer to This PC", "Open File Explorer to This PC instead of Home.", "", "Low"),
                    T("customize.notifications", "Limit notifications", "Turn off Windows toast notifications for this user account.", "", "Low"),
                }),
            }),

        "optimize" => new SectionContent(
            "OPTIMIZE", "Performance, your way.",
            "Reversible performance preferences, plus power modes and startup review. Ownly does not promise specific FPS or speed numbers.",
            false, ShowSystemSnapshot: true,
            new[]
            {
                new SectionGroup("Reversible performance settings", null, new[]
                {
                    T("optimize.visual-effects", "Best-performance visual effects", "Switch Windows animations and effects to the Best Performance profile.", "", "Low"),
                    T("optimize.game-mode", "Game Mode", "Enable the Windows Game Mode preference for this user.", "", "Low"),
                }),
                new SectionGroup("Deeper tools", "These open a focused page with their own review step.", new[]
                {
                    new SectionItem("optimize.power-mode", "Power modes", "Balanced, Best performance, or Best power efficiency — with the previous scheme logged for restore.", "", "Moderate", SectionItemKind.Navigate, NavigateTarget: "power"),
                    new SectionItem("optimize.startup", "Startup manager", "Review programs in your personal Startup folder and move any of them to a private backup.", "", "Moderate", SectionItemKind.Navigate, NavigateTarget: "startup"),
                }),
            }),

        "privacy" => new SectionContent(
            "PRIVACY", "Your privacy.",
            "Reversible per-user privacy settings. Turning one off restores the value Ownly recorded before the change.",
            false, false,
            new[]
            {
                new SectionGroup("Advertising & personalization", null, new[]
                {
                    T("privacy.advertising-id", "Limit ad personalization", "Turn off the per-user advertising ID used to personalize ads.", "", "Low"),
                    T("privacy.tailored", "Limit tailored experiences", "Stop Windows using diagnostic data to personalize tips and content.", "", "Low"),
                    T("privacy.typing", "Limit typing personalization", "Turn off implicit collection of your typing and inking for personalization.", "", "Low"),
                }),
                new SectionGroup("Activity & search", null, new[]
                {
                    T("privacy.activity-history", "Limit Activity History", "Stop Windows collecting and syncing your activity history for this user.", "", "Moderate"),
                    T("privacy.cloud-search", "Limit cloud content in search", "Keep Microsoft/work account cloud content out of Windows Search where supported.", "", "Moderate"),
                }),
                new SectionGroup("Device access", "Apps that need a device will ask again after you switch this back.", new[]
                {
                    T("privacy.camera", "Limit camera access", "Deny camera access to apps until you restore it.", "", "Moderate"),
                    T("privacy.microphone", "Limit microphone access", "Deny microphone access to apps until you restore it.", "", "Moderate"),
                    T("privacy.location", "Limit location access", "Deny location access to apps until you restore it.", "", "Moderate"),
                }),
            }),

        "tools" => new SectionContent(
            "TOOLS", "Useful tools.",
            "Focused utilities and a read-only look at this device.",
            false, ShowSystemSnapshot: true,
            new[]
            {
                new SectionGroup("Maintenance", null, new[]
                {
                    new SectionItem("tools.startup", "Startup manager", "Review what starts with Windows from your personal Startup folder.", "", "Moderate", SectionItemKind.Navigate, NavigateTarget: "startup"),
                    new SectionItem("tools.power", "Power modes", "Switch the active Windows power scheme with a restore path.", "", "Moderate", SectionItemKind.Navigate, NavigateTarget: "power"),
                    new SectionItem("tools.data-folder", "Open Ownly's data folder", "See the backups and change log Ownly keeps on this PC.", "", "Low", SectionItemKind.Action, ActionLabel: "OPEN FOLDER"),
                }),
            }),

        "changes" => new SectionContent(
            "CHANGES", "Your changes.",
            "Every change Ownly has applied on this PC, newest first. Reversible changes can be restored here.",
            false, false,
            Array.Empty<SectionGroup>()),

        "activity" => new SectionContent(
            "ACTIVITY", "What Ownly did.",
            "A running log of every action — the setting or command, whether it needed administrator rights, and any output it produced.",
            false, false,
            Array.Empty<SectionGroup>()),

        "dangerous" => new SectionContent(
            "DANGEROUS", "The danger zone.",
            "Deeper system changes and a couple of pointless ones. Still reversible — make a restore point first.",
            false, false,
            Array.Empty<SectionGroup>()),

        "help" => new SectionContent(
            "HELP", "Common problems.",
            "Answers for install issues, a tweak that didn't go as expected, and how the app fits together.",
            false, false,
            Array.Empty<SectionGroup>()),

        _ => new SectionContent(
            "OWNLY", "Ownly workspace.",
            "A focused workspace for making Windows feel like yours.",
            false, false, Array.Empty<SectionGroup>())
    };

    private static SectionItem T(string id, string title, string description, string glyph, string risk) =>
        new(id, title, description, glyph, risk, SectionItemKind.Toggle);
}
