using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Ownly.Core.Safety;
using Ownly.Core.Tweaks;

namespace Ownly.Core.Uninstall;

/// <summary>
/// Lists installed desktop programs from the standard Windows "Uninstall" registry keys and can
/// launch each program's own uninstaller. Ownly does not delete files or registry keys itself here
/// — it hands off to the vendor's uninstaller, then reports what (if anything) is left behind.
/// </summary>
public sealed class InstalledAppsService
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private static readonly string[] ProtectedTerms =
    {
        "Microsoft Edge", "Windows Security", "Microsoft Store", ".NET", "Visual C++",
        "Windows App Runtime", "Desktop App Installer", "Windows Update", "NVIDIA", "AMD", "Intel",
        "Realtek", "Ownly"
    };

    public IReadOnlyList<InstalledApp> Scan()
    {
        var results = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);
        var locations = new[]
        {
            ("HKCU", RegistryHive.CurrentUser, RegistryView.Default),
            ("HKLM64", RegistryHive.LocalMachine, RegistryView.Registry64),
            ("HKLM32", RegistryHive.LocalMachine, RegistryView.Registry32),
        };

        foreach (var (hiveLabel, hive, view) in locations)
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var uninstall = root.OpenSubKey(UninstallPath);
                if (uninstall is null) continue;

                foreach (var name in uninstall.GetSubKeyNames())
                {
                    try
                    {
                        using var app = uninstall.OpenSubKey(name);
                        if (app is null) continue;

                        var displayName = app.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(displayName)) continue;

                        // skip Windows updates/hotfixes/system components, not real "programs"
                        if (Convert.ToInt32(app.GetValue("SystemComponent", 0)) == 1) continue;
                        if (app.GetValue("ParentKeyName") is not null) continue;
                        if (app.GetValue("ReleaseType") is string rt &&
                            (rt.Contains("Update", StringComparison.OrdinalIgnoreCase) || rt.Contains("Hotfix", StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        var publisher = app.GetValue("Publisher") as string ?? "Unknown publisher";
                        var version = app.GetValue("DisplayVersion") as string ?? "";
                        var sizeKb = Convert.ToInt64(app.GetValue("EstimatedSize", 0L));
                        var installLocation = app.GetValue("InstallLocation") as string;
                        var uninstallString = app.GetValue("UninstallString") as string;
                        var quietUninstallString = app.GetValue("QuietUninstallString") as string;
                        var key = displayName.Trim() + "|" + publisher;

                        var isProtected = ProtectedTerms.Any(term =>
                            displayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            publisher.Contains(term, StringComparison.OrdinalIgnoreCase));

                        results[key] = new InstalledApp(
                            displayName.Trim(), publisher, version, sizeKb, installLocation,
                            uninstallString, quietUninstallString, hiveLabel, UninstallPath + "\\" + name,
                            isProtected,
                            isProtected ? "Ownly protects system, security, runtime, and hardware dependencies." : "");
                    }
                    catch
                    {
                        // one bad subkey should not fail the whole scan
                    }
                }
            }
            catch
            {
                // a missing or inaccessible uninstall hive is not a scan failure
            }
        }

        return results.Values.OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Launches the app's own uninstaller and waits for it to exit. Ownly cannot guarantee the
    /// uninstall completed — many uninstallers show their own window — so the caller should
    /// re-scan afterward rather than trust the process exit code alone.
    /// </summary>
    public ActionResult Uninstall(InstalledApp app)
    {
        if (!app.CanUninstall)
        {
            return new ActionResult(false, app.IsProtected
                ? "Ownly will not uninstall this: " + app.ProtectionReason
                : "Ownly has no uninstall command on record for this program.");
        }

        try
        {
            var psi = new ProcessStartInfo("cmd.exe", "/c " + app.Command)
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };
            using var process = Process.Start(psi);
            process?.WaitForExit();

            new ActivityLog().Append(
                "Uninstall " + app.DisplayName,
                app.Command,
                "Uninstaller",
                elevated: false,
                success: true,
                output: process is null ? "Started." : $"Exit code {process.ExitCode}.");

            return new ActionResult(true, $"{app.DisplayName}'s uninstaller ran. If it showed its own window, finish there, then refresh this list.");
        }
        catch (Exception ex)
        {
            new ActivityLog().Append("Uninstall " + app.DisplayName, app.Command, "Uninstaller", false, false, ex.Message);
            return new ActionResult(false, "Ownly could not start the uninstaller: " + ex.Message);
        }
    }

    /// <summary>True if the registry uninstall entry for this app is still present (a leftover
    /// after its uninstaller ran and removed the program but not its own registry trace).</summary>
    public static bool StillRegistered(InstalledApp app)
    {
        try
        {
            using var root = app.RegistryHive == "HKCU"
                ? Registry.CurrentUser
                : RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, app.RegistryHive == "HKLM64" ? RegistryView.Registry64 : RegistryView.Registry32);
            using var key = root.OpenSubKey(app.RegistryKeyPath);
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    public static bool InstallFolderStillExists(InstalledApp app) =>
        !string.IsNullOrWhiteSpace(app.InstallLocation) && Directory.Exists(app.InstallLocation);

    /// <summary>Removes only the orphaned registry uninstall entry — never touches files.</summary>
    public ActionResult RemoveRegistryTrace(InstalledApp app)
    {
        try
        {
            if (app.RegistryHive == "HKCU")
            {
                Registry.CurrentUser.DeleteSubKeyTree(app.RegistryKeyPath, throwOnMissingSubKey: false);
            }
            else
            {
                using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, app.RegistryHive == "HKLM64" ? RegistryView.Registry64 : RegistryView.Registry32);
                root.DeleteSubKeyTree(app.RegistryKeyPath, throwOnMissingSubKey: false);
            }
            new ActivityLog().Append("Remove leftover registry entry", app.DisplayName, "Registry", app.RegistryHive != "HKCU", true, "Removed " + app.RegistryKeyPath);
            return new ActionResult(true, "The leftover registry entry was removed.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, "Ownly could not remove this registry entry: " + ex.Message);
        }
    }

    public void OpenInstallFolder(InstalledApp app)
    {
        if (string.IsNullOrWhiteSpace(app.InstallLocation) || !Directory.Exists(app.InstallLocation)) return;
        Process.Start(new ProcessStartInfo(app.InstallLocation) { UseShellExecute = true });
    }
}
