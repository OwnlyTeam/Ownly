using System.Collections.Generic;
using System.Linq;

namespace Ownly.Core.Tweaks;

/// <summary>
/// The extra Windows options Ownly can apply. Registry tweaks under HKCU run silently; anything
/// that needs administrator rights runs through a Windows permission prompt and a PowerShell window.
/// Everything reversible stores its previous state so the Changes section can restore it.
/// </summary>
public static class TweakCatalog
{
    private const string Advanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string CDM = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";

    public static IReadOnlyList<Tweak> All { get; } = new List<Tweak>
    {
        // ---------------- CLEAN ----------------
        Reg("clean.web-search", "clean", "Search & Start", "Remove web results from Start search",
            "Stop the Start menu search box from sending what you type to Bing.",
            "Low", admin: false, note: "Sign out and back in to apply.",
            new RegValue("HKCU", @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", "DWord", 1)),

        Reg("clean.chat-taskbar", "clean", "Taskbar", "Hide the Chat button",
            "Remove the Chat / Teams icon from the taskbar.",
            "Low", admin: false,
            new RegValue("HKCU", Advanced, "TaskbarMn", "DWord", 0)),

        Reg("clean.widgets-taskbar", "clean", "Taskbar", "Hide the Widgets button",
            "Remove the Widgets / news-and-weather icon from the taskbar.",
            "Low", admin: false,
            new RegValue("HKCU", Advanced, "TaskbarDa", "DWord", 0)),

        Reg("clean.copilot", "clean", "AI & suggestions", "Turn off Windows Copilot",
            "Disable the Copilot button and panel for this user.",
            "Moderate", admin: false, note: "Sign out and back in to apply.",
            new RegValue("HKCU", @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", "DWord", 1)),

        Reg("clean.settings-suggestions", "clean", "AI & suggestions", "Turn off Settings app suggestions",
            "Stop the Settings app and notifications showing tips and promoted content.",
            "Low", admin: false,
            new RegValue("HKCU", CDM, "SubscribedContent-338393Enabled", "DWord", 0),
            new RegValue("HKCU", CDM, "SubscribedContent-353694Enabled", "DWord", 0),
            new RegValue("HKCU", CDM, "SubscribedContent-353696Enabled", "DWord", 0)),

        Reg("clean.consumer-features", "clean", "AI & suggestions", "Stop auto-installing suggested apps",
            "Prevent Windows from silently installing promoted third-party apps for new users.",
            "Moderate", admin: true,
            new RegValue("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", "DWord", 1)),

        // ---------------- CUSTOMIZE ----------------
        Reg("customize.seconds-clock", "customize", "Taskbar", "Show seconds in the clock",
            "Add seconds to the taskbar clock.",
            "Low", admin: false, note: "Sign out and back in to apply.",
            new RegValue("HKCU", Advanced, "ShowSecondsInSystemClock", "DWord", 1)),

        Reg("customize.end-task", "customize", "Taskbar", "Add \"End task\" to the taskbar menu",
            "Let you kill an app straight from its taskbar right-click menu.",
            "Low", admin: false,
            new RegValue("HKCU", @"Software\Microsoft\Windows\CurrentVersion\TaskbarDeveloperSettings", "TaskbarEndTask", "DWord", 1)),

        Reg("customize.transparency", "customize", "Appearance", "Turn off transparency effects",
            "Use solid surfaces instead of the translucent acrylic effect.",
            "Low", admin: false,
            new RegValue("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", "DWord", 0)),

        Reg("customize.desktop-icons", "customize", "Desktop", "Show This PC and Recycle Bin on the desktop",
            "Add the classic This PC and Recycle Bin icons to the desktop.",
            "Low", admin: false,
            new RegValue("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", "DWord", 0),
            new RegValue("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{645FF040-5081-101B-9F08-00AA002F954E}", "DWord", 0)),

        Reg("customize.verbose-status", "customize", "Sign-in", "Show detailed startup messages",
            "Display what Windows is doing during start-up, shutdown, sign-in and sign-out.",
            "Low", admin: true,
            new RegValue("HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "verbosestatus", "DWord", 1)),

        // ---------------- OPTIMIZE ----------------
        Reg("optimize.menu-delay", "optimize", "Responsiveness", "Remove the menu open delay",
            "Make menus appear instantly instead of after a short pause.",
            "Low", admin: false, note: "Sign out and back in to apply.",
            new RegValue("HKCU", @"Control Panel\Desktop", "MenuShowDelay", "String", Text: "0")),

        Reg("optimize.startup-delay", "optimize", "Startup", "Remove the startup-app delay",
            "Let programs that start with Windows launch immediately rather than after a built-in delay.",
            "Low", admin: false,
            new RegValue("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", "DWord", 0)),

        Reg("optimize.fast-startup-off", "optimize", "Boot", "Turn off Fast Startup",
            "Do a full shutdown every time. Slower to boot but avoids the driver and update quirks Fast Startup can cause.",
            "Moderate", admin: true,
            new RegValue("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", "DWord", 0)),

        Act("optimize.hibernate-off", "optimize", "Boot", "Disable hibernation",
            "Turn off hibernation and delete the hidden hiberfil.sys file to reclaim disk space. Also disables Fast Startup.",
            "Moderate", admin: true, reversible: true,
            apply: "powercfg /hibernate off; Write-Output 'Hibernation disabled.'",
            revert: "powercfg /hibernate on; Write-Output 'Hibernation re-enabled.'"),

        Act("optimize.ultimate-performance", "optimize", "Power", "Add the Ultimate Performance power plan",
            "Unlock the hidden Ultimate Performance plan. Select it afterwards from Power Options.",
            "Moderate", admin: true, reversible: false,
            note: "Remove later from Control Panel → Power Options if you want.",
            apply: "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61"),

        Act("optimize.diagtrack-off", "optimize", "Services", "Disable the telemetry service",
            "Stop and disable the Connected User Experiences and Telemetry (DiagTrack) service.",
            "Moderate", admin: true, reversible: true,
            apply: "Stop-Service DiagTrack -Force -EA SilentlyContinue; Set-Service DiagTrack -StartupType Disabled; Write-Output 'DiagTrack disabled.'",
            revert: "Set-Service DiagTrack -StartupType Automatic; Start-Service DiagTrack -EA SilentlyContinue; Write-Output 'DiagTrack restored.'"),

        // ---------------- PRIVACY ----------------
        Reg("privacy.telemetry-min", "privacy", "Diagnostics", "Set diagnostic data to the minimum",
            "Request the lowest telemetry level Windows will honour on this edition.",
            "Moderate", admin: true,
            new RegValue("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", "DWord", 0)),

        Reg("privacy.online-speech", "privacy", "Speech & input", "Turn off online speech recognition",
            "Stop sending your voice data to Microsoft for cloud speech recognition.",
            "Low", admin: false,
            new RegValue("HKCU", @"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy", "HasAccepted", "DWord", 0)),

        Reg("privacy.app-launch-tracking", "privacy", "Personalization", "Stop tracking which apps you open",
            "Turn off the usage tracking that powers \"most used\" lists in Start and Search.",
            "Low", admin: false,
            new RegValue("HKCU", Advanced, "Start_TrackProgs", "DWord", 0)),

        Reg("privacy.feedback-off", "privacy", "Diagnostics", "Never ask for feedback",
            "Stop Windows periodically prompting you for feedback.",
            "Low", admin: false,
            new RegValue("HKCU", @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", "DWord", 0)),

        Reg("privacy.advertising-id-machine", "privacy", "Advertising", "Block the advertising ID for all users",
            "Disable the advertising ID at the machine level, on top of the per-user setting.",
            "Low", admin: true,
            new RegValue("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy", "DWord", 1)),

        // ---------------- TOOLS ----------------
        Act("tools.flush-dns", "tools", "Network", "Flush the DNS cache",
            "Clear the resolver cache. Helps when a site loads on your phone but not this PC.",
            "Low", admin: false, reversible: false,
            apply: "ipconfig /flushdns"),

        Act("tools.restart-explorer", "tools", "Maintenance", "Restart Windows Explorer",
            "Restart the desktop, taskbar and File Explorer. Applies many of the taskbar options above without a sign-out.",
            "Low", admin: false, reversible: false, wait: false,
            apply: "Stop-Process -Name explorer -Force; Start-Sleep 1; if (-not (Get-Process explorer -EA SilentlyContinue)) { Start-Process explorer }"),

        Act("tools.empty-recycle-bin", "tools", "Storage", "Empty the Recycle Bin",
            "Permanently delete everything currently in the Recycle Bin.",
            "Moderate", admin: false, reversible: false,
            apply: "Clear-RecycleBin -Force -ErrorAction SilentlyContinue; Write-Output 'Recycle Bin emptied.'"),

        Act("tools.clear-update-cache", "tools", "Storage", "Clear the Windows Update cache",
            "Stop Windows Update, delete its downloaded-installer cache, then restart it. Updates re-download as needed.",
            "Moderate", admin: true, reversible: false,
            apply: "Stop-Service wuauserv,bits -Force -EA SilentlyContinue; Remove-Item \"$env:SystemRoot\\SoftwareDistribution\\Download\\*\" -Recurse -Force -EA SilentlyContinue; Start-Service wuauserv,bits -EA SilentlyContinue; Write-Output 'Update cache cleared.'"),

        Act("tools.restore-point", "tools", "Safety", "Create a system restore point",
            "Make a Windows restore point called \"Ownly\" so you can roll the whole system back if needed.",
            "Moderate", admin: true, reversible: false, wait: true, timeoutSeconds: 240,
            apply: "Enable-ComputerRestore -Drive $env:SystemDrive -EA SilentlyContinue; Checkpoint-Computer -Description 'Ownly' -RestorePointType 'MODIFY_SETTINGS'; Write-Output 'Restore point created.'"),

        Act("tools.sfc", "tools", "Repair", "Run System File Checker (SFC)",
            "Scan Windows system files for corruption and repair them. This can take 10–15 minutes. Ownly stays usable while it runs; the full output lands in Activity.",
            "Moderate", admin: true, reversible: false, wait: true, timeoutSeconds: 2400,
            apply: "sfc /scannow"),

        Act("tools.dism", "tools", "Repair", "Repair the Windows image (DISM)",
            "Use Windows Update to repair the component store. Run this before SFC if SFC cannot fix everything. Can take 10–20 minutes; output lands in Activity.",
            "Moderate", admin: true, reversible: false, wait: true, timeoutSeconds: 2400,
            apply: "DISM /Online /Cleanup-Image /RestoreHealth"),
    };

    public static IReadOnlyList<Tweak> ForSection(string section) =>
        All.Where(t => t.Section == section).ToList();

    // ---- builders ----

    private static Tweak Reg(string id, string section, string group, string title, string description,
        string risk, bool admin, params RegValue[] values) =>
        Reg(id, section, group, title, description, risk, admin, note: null, values);

    private static Tweak Reg(string id, string section, string group, string title, string description,
        string risk, bool admin, string? note, params RegValue[] values) =>
        new(id, section, group, title, description, risk, TweakKind.Registry, admin, Reversible: true, note, values);

    private static Tweak Act(string id, string section, string group, string title, string description,
        string risk, bool admin, bool reversible, string apply, string? revert = null, string? note = null,
        bool wait = true, int timeoutSeconds = 90) =>
        new(id, section, group, title, description, risk, TweakKind.Action, admin, reversible, note,
            Registry: null, ActionApply: apply, ActionRevert: revert, ActionWait: wait, ActionShowWindow: false,
            ActionTimeoutSeconds: timeoutSeconds);
}
