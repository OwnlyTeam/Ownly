using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Windows.Management.Deployment;

namespace Ownly.Core.Scanning;

/// <summary>
/// Performs safe, read-only discovery for the first Ownly scanner milestone.
/// It never deletes, disables, or changes anything on the device.
/// </summary>
public sealed class ReadOnlyScanner
{
    public ScanReport Scan()
    {
        var temporaryBytes = MeasureTemporaryFiles();
        var startupEntries = ReadStartupEntries();
        var startupItems = startupEntries.Count;
        var bloatwareCandidates = FindBloatwareCandidates();
        var recommendations = new List<ScanRecommendation>();

        if (temporaryBytes >= 500 * 1024 * 1024)
        {
            recommendations.Add(new ScanRecommendation(
                "Review temporary files",
                "Temporary data is using a noticeable amount of storage.",
                $"{ToGigabytes(temporaryBytes):0.0} GB found in your user temp folder.",
                "CLEAN"));
        }

        if (startupItems > 0)
        {
            recommendations.Add(new ScanRecommendation(
                "Review startup items",
                "Some applications are configured to start with Windows.",
                $"{startupItems} item{(startupItems == 1 ? "" : "s")} found in your personal Startup folder.",
                "OPTIMIZE"));
        }

        if (bloatwareCandidates.Count > 0)
        {
            recommendations.Add(new ScanRecommendation(
                "Review optional software",
                "Ownly found installed apps that may be optional for your setup.",
                $"{bloatwareCandidates.Count} candidate{(bloatwareCandidates.Count == 1 ? "" : "s")} found. Nothing has been removed.",
                "BLOATWARE"));
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new ScanRecommendation(
                "Your first scan is clear",
                "Ownly did not find any basic recommendations yet.",
                "More read-only checks will arrive as the scanner grows.",
                "READY"));
        }

        return new ScanReport(recommendations, temporaryBytes, startupItems, bloatwareCandidates, startupEntries);
    }

    private static long MeasureTemporaryFiles()
    {
        try
        {
            var tempPath = Path.GetTempPath();
            return new DirectoryInfo(tempPath)
                .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Take(5000)
                .Sum(file =>
                {
                    try { return file.Length; }
                    catch { return 0L; }
                });
        }
        catch
        {
            return 0L;
        }
    }

    private static IReadOnlyList<StartupEntry> ReadStartupEntries()
    {
        try
        {
            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (string.IsNullOrWhiteSpace(startupPath) || !Directory.Exists(startupPath))
            {
                return Array.Empty<StartupEntry>();
            }

            return Directory.EnumerateFiles(startupPath)
                .Take(100)
                .Select(path => new StartupEntry(Path.GetFileName(path), path))
                .ToList();
        }
        catch
        {
            return Array.Empty<StartupEntry>();
        }
    }

    private static IReadOnlyList<BloatwareCandidate> FindBloatwareCandidates()
    {
        var keywords = new[]
        {
            "Candy Crush", "Clipchamp", "LinkedIn", "TikTok", "Facebook", "Disney",
            "WildTangent", "McAfee", "Norton", "Dropbox", "Spotify", "Booking.com",
            "Xbox", "Solitaire", "Dolby", "ExpressVPN"
        };
        var candidates = new Dictionary<string, BloatwareCandidate>(StringComparer.OrdinalIgnoreCase);
        var uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        var locations = new[]
        {
            (RegistryHive.CurrentUser, RegistryView.Default),
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32)
        };

        foreach (var (hive, view) in locations)
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var uninstall = root.OpenSubKey(uninstallPath);
                if (uninstall is null)
                {
                    continue;
                }

                foreach (var name in uninstall.GetSubKeyNames())
                {
                    using var app = uninstall.OpenSubKey(name);
                    var displayName = app?.GetValue("DisplayName") as string;
                    if (!string.IsNullOrWhiteSpace(displayName) && keywords.Any(keyword => displayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        var normalizedName = displayName.Trim();
                        var publisher = app?.GetValue("Publisher") as string ?? "Unknown publisher";
                        var protection = GetProtection(normalizedName, publisher);
                        candidates[normalizedName] = new BloatwareCandidate(normalizedName, "Desktop app", publisher, app?.GetValue("UninstallString") as string, protection.IsProtected, protection.Reason);
                    }
                }
            }
            catch
            {
                // A missing or inaccessible uninstall hive is not a scan failure.
            }
        }

        try
        {
            var packageManager = new PackageManager();
            foreach (var package in packageManager.FindPackagesForUser(string.Empty))
            {
                var displayName = package.DisplayName;
                if (!string.IsNullOrWhiteSpace(displayName) && keywords.Any(keyword => displayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    var normalizedName = displayName.Trim();
                    var publisher = package.Id.Publisher ?? "Microsoft Store package";
                    var protection = GetProtection(normalizedName, publisher);
                    candidates[normalizedName] = new BloatwareCandidate(normalizedName, "Store package", publisher, null, protection.IsProtected, protection.Reason);
                }
            }
        }
        catch
        {
            // Package inventory can be unavailable in an unpackaged or restricted context.
        }

        return candidates.Values.OrderBy(candidate => candidate.DisplayName).Take(30).ToList();
    }

    private static (bool IsProtected, string Reason) GetProtection(string displayName, string publisher)
    {
        var protectedTerms = new[]
        {
            "Microsoft Edge", "Windows Security", "Microsoft Store", ".NET", "Visual C++",
            "Windows App Runtime", "Desktop App Installer", "Windows Update", "NVIDIA", "AMD", "Intel"
        };
        var isProtected = protectedTerms.Any(term => displayName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || publisher.Contains(term, StringComparison.OrdinalIgnoreCase));
        return isProtected
            ? (true, "Ownly protects system, security, runtime, and hardware dependencies.")
            : (false, string.Empty);
    }

    private static double ToGigabytes(long bytes) => bytes / 1024d / 1024d / 1024d;
}
