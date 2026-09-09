using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ownly.Core.Clean;
using Ownly.Core.Optimize;

namespace Ownly.Core.Safety;

/// <summary>
/// Ownly's first reversible action. The engine records the exact prior state
/// before changing a per-user preference and can restore it later.
/// </summary>
public sealed class SafetyEngine
{
    private const string ExplorerAdvancedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string HideFileExtensionsValue = "HideFileExt";
    private const string HiddenFilesValue = "Hidden";
    private const string ExplorerLaunchToValue = "LaunchTo";
    private const string TaskbarAlignmentValue = "TaskbarAl";
    private const string ClassicMenuPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
    private const string PersonalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AdvertisingPath = @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
    private const string PrivacyPath = @"Software\Microsoft\Windows\CurrentVersion\Privacy";
    private const string GameBarPath = @"Software\Microsoft\GameBar";
    private const string VisualEffectsPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
    private const string NotificationsPath = @"Software\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string SearchSettingsPath = @"Software\Microsoft\Windows\CurrentVersion\SearchSettings";
    private const string TypingPersonalizationPath = @"Software\Microsoft\InputPersonalization";
    private const string SuggestionsPath = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
    private static readonly string[] SuggestionValues =
    {
        "SystemPaneSuggestionsEnabled",
        "SubscribedContent-338388Enabled",
        "SubscribedContent-338389Enabled",
        "SoftLandingEnabled",
        "RotatingLockScreenEnabled",
        "RotatingLockScreenOverlayEnabled"
    };
    private const string ActivityPath = @"Software\Microsoft\Windows\CurrentVersion\ActivityFeed";
    private const string BackgroundAppsPath = @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications";
    private const string CameraPermissionPath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\camera";
    private const string MicrophonePermissionPath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone";
    private const string LocationPermissionPath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
    private static readonly string[] ActivityValues =
    {
        "EnableActivityFeed",
        "PublishUserActivities",
        "UploadUserActivities"
    };
    private static readonly string[] CloudSearchValues =
    {
        "IsAADCloudSearchEnabled",
        "IsMSACloudSearchEnabled"
    };
    private static readonly string[] TypingPersonalizationValues =
    {
        "RestrictImplicitInkCollection",
        "RestrictImplicitTextCollection"
    };
    private readonly ChangeLogService _changeLog;

    public SafetyEngine(ChangeLogService? changeLog = null)
    {
        _changeLog = changeLog ?? new ChangeLogService();
    }

    public ActionResult ApplyShowFileExtensions()
    {
        var before = ReadFileExtensionState();
        if (before == "0")
        {
            return new ActionResult(true, "File extensions are already visible.");
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedPath, writable: true)
                ?? throw new InvalidOperationException("The Explorer preference could not be opened.");
            key.SetValue(HideFileExtensionsValue, 0, RegistryValueKind.DWord);
            var change = new ChangeRecord(
                Guid.NewGuid().ToString("N"),
                "customize.file-extensions",
                "Show file extensions",
                "Customize",
                "Low",
                DateTimeOffset.Now,
                before,
                "0",
                true,
                "File Explorer will show the extension for each file.");
            _changeLog.Record(change);
            return new ActionResult(true, "File extensions are now set to show.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save this preference: {exception.Message}");
        }
    }

    public ActionResult ApplyShowHiddenFiles() => ApplyDwordPreference(
        "customize.hidden-files", "Show hidden files", "Customize", ExplorerAdvancedPath, HiddenFilesValue, 1,
        "Low", "Hidden files are already visible.", "Hidden files are now visible in File Explorer.",
        "File Explorer will show hidden files for this user. Protected operating-system files remain controlled separately.");

    public ActionResult ApplyLeftTaskbarAlignment() => ApplyDwordPreference(
        "customize.taskbar", "Left taskbar alignment", "Customize", ExplorerAdvancedPath, TaskbarAlignmentValue, 0,
        "Low", "The taskbar already prefers left alignment.", "The taskbar now prefers left alignment for this user.",
        "Taskbar alignment is set to the left when supported by this Windows version.");

    public ActionResult ApplyDisableNotifications() => ApplyDwordPreference(
        "customize.notifications", "Limit notifications", "Customize", NotificationsPath, "ToastEnabled", 0,
        "Low", "Notifications are already limited for this user.", "Notifications are now limited for this user.",
        "Windows toast notifications are disabled for this user account until restored.");

    public ActionResult ApplyExplorerToThisPc() => ApplyDwordPreference(
        "customize.explorer-home", "Open Explorer to This PC", "Customize", ExplorerAdvancedPath, ExplorerLaunchToValue, 1,
        "Low", "File Explorer already opens to This PC.", "File Explorer now opens to This PC.",
        "File Explorer will open to This PC for this user when supported by Windows.");

    public ActionResult ApplyDarkMode()
    {
        var before = $"AppsUseLightTheme={ReadDword(PersonalizePath, "AppsUseLightTheme")};SystemUsesLightTheme={ReadDword(PersonalizePath, "SystemUsesLightTheme")}";
        if (before == "AppsUseLightTheme=0;SystemUsesLightTheme=0")
        {
            return new ActionResult(true, "Dark mode is already enabled.");
        }

        try
        {
            SetDword(PersonalizePath, "AppsUseLightTheme", 0);
            SetDword(PersonalizePath, "SystemUsesLightTheme", 0);
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "customize.theme", "Dark mode", "Customize", "Low", DateTimeOffset.Now, before, "AppsUseLightTheme=0;SystemUsesLightTheme=0", true, "Windows apps and system surfaces will prefer dark mode.");
            _changeLog.Record(change);
            return new ActionResult(true, "Dark mode is now enabled for your user account.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save the theme preference: {exception.Message}");
        }
    }

    public ActionResult ApplyClassicContextMenu()
    {
        var before = "missing";
        try
        {
            using (var existing = Registry.CurrentUser.OpenSubKey(ClassicMenuPath, writable: false))
            {
                if (existing is not null)
                {
                    if (existing.GetValueNames().Length > 0 || existing.GetSubKeyNames().Length > 0)
                    {
                        return new ActionResult(false, "Ownly found an existing custom context-menu registration and will not overwrite it.");
                    }
                    before = "empty";
                }
            }

            using var key = Registry.CurrentUser.CreateSubKey(ClassicMenuPath, writable: true)
                ?? throw new InvalidOperationException("The context-menu preference could not be opened.");
            key.SetValue(string.Empty, string.Empty, RegistryValueKind.String);
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "customize.compact-menu", "Classic context menu", "Customize", "Low", DateTimeOffset.Now, before, "created", true, "The classic Windows context menu override was created for this user.");
            _changeLog.Record(change);
            return new ActionResult(true, "Classic context menu is enabled. Restart File Explorer to see it.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save the context-menu preference: {exception.Message}");
        }
    }

    public ActionResult ApplyDisableAdvertisingId()
    {
        var before = ReadDword(AdvertisingPath, "Enabled");
        if (before == "0")
        {
            return new ActionResult(true, "Advertising personalization is already limited.");
        }

        try
        {
            SetDword(AdvertisingPath, "Enabled", 0);
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "privacy.advertising-id", "Limit advertising personalization", "Privacy", "Low", DateTimeOffset.Now, before, "0", true, "Windows advertising ID personalization is disabled for this user.");
            _changeLog.Record(change);
            return new ActionResult(true, "Advertising personalization is now limited for your user account.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save the privacy preference: {exception.Message}");
        }
    }

    public ActionResult ApplyDisableTailoredExperiences() => ApplyDwordPreference(
        "privacy.tailored", "Limit tailored experiences", "Privacy", PrivacyPath, "TailoredExperiencesWithDiagnosticDataEnabled", 0,
        "Low", "Windows tailored experiences are already limited.", "Tailored experiences are now limited for your user account.",
        "Windows personalization based on optional diagnostic data is disabled.");

    public ActionResult ApplyGameMode() => ApplyDwordPreference(
        "optimize.game-mode", "Enable Game Mode", "Optimize", GameBarPath, "AutoGameModeEnabled", 1,
        "Low", "Game Mode is already enabled.", "Game Mode is now enabled for your user account.",
        "Windows Game Mode is enabled; this does not guarantee an FPS increase.");

    public ActionResult ApplyBestPerformanceVisuals() => ApplyDwordPreference(
        "optimize.visual-effects", "Best Performance visual effects", "Optimize", VisualEffectsPath, "VisualFXSetting", 2,
        "Low", "Best Performance visual effects are already selected.", "Best Performance visual effects are now selected for your user account.",
        "Windows visual effects use the Best Performance profile; this does not change application settings.");

    public ActionResult ApplyDisableWindowsSuggestions()
    {
        var before = ReadCompoundState(SuggestionsPath, SuggestionValues);
        if (SuggestionValues.All(valueName => ReadStatePart(before, valueName) == "0"))
        {
            return new ActionResult(true, "Windows suggestions are already limited.");
        }

        try
        {
            foreach (var valueName in SuggestionValues)
            {
                SetDword(SuggestionsPath, valueName, 0);
            }
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "clean.windows-suggestions", "Limit Windows suggestions", "Clean", "Low", DateTimeOffset.Now, before, ReadCompoundState(SuggestionsPath, SuggestionValues), true, "Optional Windows recommendations and promotional surfaces are limited for this user.");
            _changeLog.Record(change);
            return new ActionResult(true, "Windows suggestions are now limited for your user account.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save the suggestions preference: {exception.Message}");
        }
    }

    public ActionResult ApplyDisableActivityHistory()
    {
        var before = ReadCompoundState(ActivityPath, ActivityValues);
        if (ActivityValues.All(valueName => ReadStatePart(before, valueName) == "0"))
        {
            return new ActionResult(true, "Activity History publishing is already limited.");
        }

        try
        {
            foreach (var valueName in ActivityValues)
            {
                SetDword(ActivityPath, valueName, 0);
            }
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "privacy.activity-history", "Limit Activity History", "Privacy", "Moderate", DateTimeOffset.Now, before, ReadCompoundState(ActivityPath, ActivityValues), true, "Activity History publishing and sync are limited for this user.");
            _changeLog.Record(change);
            return new ActionResult(true, "Activity History publishing is now limited for your user account.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save the Activity History preference: {exception.Message}");
        }
    }

    public ActionResult ApplyDisableCloudSearch()
    {
        var before = ReadCompoundState(SearchSettingsPath, CloudSearchValues);
        if (CloudSearchValues.All(valueName => ReadStatePart(before, valueName) == "0"))
        {
            return new ActionResult(true, "Cloud content search is already limited.");
        }

        try
        {
            foreach (var valueName in CloudSearchValues)
            {
                SetDword(SearchSettingsPath, valueName, 0);
            }
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "privacy.cloud-search", "Limit cloud content search", "Privacy", "Moderate", DateTimeOffset.Now, before, ReadCompoundState(SearchSettingsPath, CloudSearchValues), true, "Windows search cloud content integration is limited for this user where supported.");
            _changeLog.Record(change);
            return new ActionResult(true, "Cloud content search is now limited for your user account.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save the cloud search preference: {exception.Message}");
        }
    }

    public ActionResult ApplyLimitTypingPersonalization()
    {
        var before = ReadCompoundState(TypingPersonalizationPath, TypingPersonalizationValues);
        if (TypingPersonalizationValues.All(valueName => ReadStatePart(before, valueName) == "1"))
        {
            return new ActionResult(true, "Typing personalization is already limited.");
        }

        try
        {
            foreach (var valueName in TypingPersonalizationValues)
            {
                SetDword(TypingPersonalizationPath, valueName, 1);
            }
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "privacy.typing", "Limit typing personalization", "Privacy", "Low", DateTimeOffset.Now, before, ReadCompoundState(TypingPersonalizationPath, TypingPersonalizationValues), true, "Implicit inking and typing collection are limited for this user.");
            _changeLog.Record(change);
            return new ActionResult(true, "Typing personalization is now limited for your user account.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save the typing personalization preference: {exception.Message}");
        }
    }

    public ActionResult ApplyLimitBackgroundApps() => ApplyDwordPreference(
        "clean.background-apps", "Limit background apps", "Clean", BackgroundAppsPath, "GlobalUserDisabled", 1,
        "Moderate", "Background app activity is already limited.", "Background app activity is now limited for your user account.",
        "Windows background activity is limited globally for this user; foreground apps continue to work.");

    public ActionResult ApplyLimitCamera() => ApplyStringPreference(
        "privacy.camera", "Limit camera access", "Privacy", CameraPermissionPath, "Value", "Deny", "Moderate",
        "Camera access is already limited.", "Camera access is now limited for your user account.",
        "Camera access is denied for apps until the previous permission is restored.");

    public ActionResult ApplyLimitMicrophone() => ApplyStringPreference(
        "privacy.microphone", "Limit microphone access", "Privacy", MicrophonePermissionPath, "Value", "Deny", "Moderate",
        "Microphone access is already limited.", "Microphone access is now limited for your user account.",
        "Microphone access is denied for apps until the previous permission is restored.");

    public ActionResult ApplyLimitLocation() => ApplyStringPreference(
        "privacy.location", "Limit location access", "Privacy", LocationPermissionPath, "Value", "Deny", "Moderate",
        "Location access is already limited.", "Location access is now limited for your user account.",
        "Location access is denied for apps until the previous permission is restored.");

    public ActionResult QuarantineTemporaryFiles()
    {
        var service = new TemporaryFileService();
        var preview = service.Preview();
        if (preview.FileCount == 0)
        {
            return new ActionResult(true, "No eligible temporary files were found.");
        }

        try
        {
            var result = service.Quarantine();
            if (!result.Success || string.IsNullOrWhiteSpace(result.BackupPath))
            {
                return new ActionResult(false, result.Message);
            }

            var change = new ChangeRecord(
                Guid.NewGuid().ToString("N"),
                "clean.temp",
                "Clean temporary files",
                "Clean",
                "Low",
                DateTimeOffset.Now,
                result.TempRoot,
                result.BackupPath,
                true,
                $"{result.MovedCount} temporary file{(result.MovedCount == 1 ? "" : "s")} moved to Ownly's private backup. Restore is available from Changes.");
            _changeLog.Record(change);
            return new ActionResult(true, result.Message, change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not clean temporary files: {exception.Message}");
        }
    }

    public ActionResult ApplyPowerMode(PowerModeDefinition mode)
    {
        var service = new PowerModeService();
        var before = service.ReadActive();
        if (string.Equals(before.Guid, mode.Guid, StringComparison.OrdinalIgnoreCase))
        {
            return new ActionResult(true, $"{mode.Name} is already active.");
        }
        if (!service.ApplyGuid(mode.Guid))
        {
            return new ActionResult(false, $"Windows could not activate the {mode.Name} power mode.");
        }

        var change = new ChangeRecord(Guid.NewGuid().ToString("N"), "optimize.power-mode", mode.Name, "Optimize", "Moderate", DateTimeOffset.Now, before.Guid, mode.Guid, true, $"Power mode changed to {mode.Name}. The previous scheme can be restored from Changes.");
        _changeLog.Record(change);
        return new ActionResult(true, $"{mode.Name} is now active.", change);
    }

    public ActionResult DisableStartupItem(string sourcePath, string displayName)
    {
        try
        {
            var startupRoot = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
            var source = Path.GetFullPath(sourcePath);
            if (!source.StartsWith(startupRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return new ActionResult(false, "Ownly can only manage entries in your personal Startup folder.");
            }
            if (!File.Exists(source))
            {
                return new ActionResult(false, "That startup entry is no longer present.");
            }
            if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            {
                return new ActionResult(false, "Ownly will not move a linked or redirected startup entry.");
            }

            var id = Guid.NewGuid().ToString("N");
            var backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ownly", "StartupBackups", id);
            Directory.CreateDirectory(backupDirectory);
            var backupPath = Path.Combine(backupDirectory, Path.GetFileName(source));
            File.Move(source, backupPath);
            var change = new ChangeRecord(id, "optimize.startup-item", displayName, "Optimize", "Moderate", DateTimeOffset.Now, source, backupPath, true, "The startup entry was moved to Ownly's private backup so it can be restored.");
            _changeLog.Record(change);
            return new ActionResult(true, $"{displayName} will no longer start automatically. It can be restored from Changes.", change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not disable this startup entry: {exception.Message}");
        }
    }

    public ActionResult Restore(ChangeRecord change)
    {
        if (!string.Equals(change.FeatureId, "customize.file-extensions", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "customize.hidden-files", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "customize.taskbar", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "customize.notifications", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "customize.explorer-home", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "customize.theme", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "customize.compact-menu", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.advertising-id", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.tailored", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.typing", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.cloud-search", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "optimize.game-mode", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "optimize.visual-effects", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "clean.windows-suggestions", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.activity-history", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "optimize.startup-item", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "clean.background-apps", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "optimize.power-mode", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.camera", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.microphone", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "privacy.location", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(change.FeatureId, "clean.temp", StringComparison.OrdinalIgnoreCase))
        {
            return new ActionResult(false, "This change cannot be restored by the current safety module.");
        }

        try
        {
            if (string.Equals(change.FeatureId, "customize.theme", StringComparison.OrdinalIgnoreCase))
            {
                RestoreDword(PersonalizePath, "AppsUseLightTheme", ReadStatePart(change.BeforeState, "AppsUseLightTheme"));
                RestoreDword(PersonalizePath, "SystemUsesLightTheme", ReadStatePart(change.BeforeState, "SystemUsesLightTheme"));
            }
            else if (string.Equals(change.FeatureId, "customize.compact-menu", StringComparison.OrdinalIgnoreCase))
            {
                if (change.BeforeState == "missing")
                {
                    using var parent = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", writable: true);
                    parent?.DeleteSubKeyTree("InprocServer32", throwOnMissingSubKey: false);
                }
                else
                {
                    using var key = Registry.CurrentUser.OpenSubKey(ClassicMenuPath, writable: true);
                    key?.DeleteValue(string.Empty, throwOnMissingValue: false);
                }
            }
            else if (string.Equals(change.FeatureId, "clean.windows-suggestions", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var valueName in SuggestionValues)
                {
                    RestoreDword(SuggestionsPath, valueName, ReadStatePart(change.BeforeState, valueName));
                }
            }
            else if (string.Equals(change.FeatureId, "privacy.activity-history", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var valueName in ActivityValues)
                {
                    RestoreDword(ActivityPath, valueName, ReadStatePart(change.BeforeState, valueName));
                }
            }
            else if (string.Equals(change.FeatureId, "privacy.cloud-search", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var valueName in CloudSearchValues)
                {
                    RestoreDword(SearchSettingsPath, valueName, ReadStatePart(change.BeforeState, valueName));
                }
            }
            else if (string.Equals(change.FeatureId, "privacy.typing", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var valueName in TypingPersonalizationValues)
                {
                    RestoreDword(TypingPersonalizationPath, valueName, ReadStatePart(change.BeforeState, valueName));
                }
            }
            else if (string.Equals(change.FeatureId, "optimize.startup-item", StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(change.AfterState))
                {
                    return new ActionResult(false, "The Ownly startup backup could not be found.");
                }
                if (File.Exists(change.BeforeState))
                {
                    return new ActionResult(false, "A startup entry already exists at the original location.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(change.BeforeState)!);
                File.Move(change.AfterState, change.BeforeState);
            }
            else if (string.Equals(change.FeatureId, "optimize.power-mode", StringComparison.OrdinalIgnoreCase))
            {
                if (!new PowerModeService().ApplyGuid(change.BeforeState))
                {
                    return new ActionResult(false, "Windows could not restore the previous power mode.");
                }
            }
            else if (string.Equals(change.FeatureId, "privacy.camera", StringComparison.OrdinalIgnoreCase))
            {
                RestoreString(CameraPermissionPath, "Value", change.BeforeState);
            }
            else if (string.Equals(change.FeatureId, "privacy.microphone", StringComparison.OrdinalIgnoreCase))
            {
                RestoreString(MicrophonePermissionPath, "Value", change.BeforeState);
            }
            else if (string.Equals(change.FeatureId, "privacy.location", StringComparison.OrdinalIgnoreCase))
            {
                RestoreString(LocationPermissionPath, "Value", change.BeforeState);
            }
            else if (string.Equals(change.FeatureId, "clean.temp", StringComparison.OrdinalIgnoreCase))
            {
                var result = new TemporaryFileService().Restore(change.AfterState, change.BeforeState);
                if (!result.Success)
                {
                    return new ActionResult(false, result.Message);
                }
            }
            else
            {
                var path = change.FeatureId switch
                {
                    "privacy.advertising-id" => AdvertisingPath,
                    "privacy.tailored" => PrivacyPath,
                    "optimize.game-mode" => GameBarPath,
                    "optimize.visual-effects" => VisualEffectsPath,
                    "clean.background-apps" => BackgroundAppsPath,
                    "customize.hidden-files" => ExplorerAdvancedPath,
                    "customize.taskbar" => ExplorerAdvancedPath,
                    "customize.notifications" => NotificationsPath,
                    "customize.explorer-home" => ExplorerAdvancedPath,
                    _ => ExplorerAdvancedPath
                };
                var valueName = change.FeatureId switch
                {
                    "privacy.advertising-id" => "Enabled",
                    "privacy.tailored" => "TailoredExperiencesWithDiagnosticDataEnabled",
                    "optimize.game-mode" => "AutoGameModeEnabled",
                    "optimize.visual-effects" => "VisualFXSetting",
                    "clean.background-apps" => "GlobalUserDisabled",
                    "customize.hidden-files" => HiddenFilesValue,
                    "customize.taskbar" => TaskbarAlignmentValue,
                    "customize.notifications" => "ToastEnabled",
                    "customize.explorer-home" => ExplorerLaunchToValue,
                    _ => HideFileExtensionsValue
                };
                RestoreDword(path, valueName, change.BeforeState);
            }

            _changeLog.MarkRestored(change.Id);
            return new ActionResult(true, $"The previous {change.FeatureName.ToLowerInvariant()} preference was restored.");
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not restore this preference: {exception.Message}");
        }
    }

    private static string ReadFileExtensionState()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedPath, writable: false);
            var rawValue = key?.GetValue(HideFileExtensionsValue);
            return rawValue is null ? "missing" : Convert.ToInt32(rawValue).ToString();
        }
        catch
        {
            return "missing";
        }
    }

    private static string ReadDword(string path, string valueName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path, writable: false);
            var rawValue = key?.GetValue(valueName);
            return rawValue is null ? "missing" : Convert.ToInt32(rawValue).ToString();
        }
        catch
        {
            return "missing";
        }
    }

    private static string ReadCompoundState(string path, IEnumerable<string> valueNames)
    {
        return string.Join(';', valueNames.Select(valueName => $"{valueName}={ReadDword(path, valueName)}"));
    }

    private static string ReadString(string path, string valueName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(path, writable: false);
            return key?.GetValue(valueName) as string ?? "missing";
        }
        catch
        {
            return "missing";
        }
    }

    private static void SetString(string path, string valueName, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException("The per-user capability preference could not be opened.");
        key.SetValue(valueName, value, RegistryValueKind.String);
    }

    private static void RestoreString(string path, string valueName, string state)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException("The per-user capability preference could not be opened.");
        if (state == "missing")
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
        else
        {
            key.SetValue(valueName, state, RegistryValueKind.String);
        }
    }

    private static void SetDword(string path, string valueName, int value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException("The per-user Windows preference could not be opened.");
        key.SetValue(valueName, value, RegistryValueKind.DWord);
    }

    private static string ReadStatePart(string state, string name)
    {
        var part = state.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(item => item.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase));
        return part is null ? "missing" : part[(name.Length + 1)..];
    }

    private static void RestoreDword(string path, string valueName, string state)
    {
        using var key = Registry.CurrentUser.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException("The per-user Windows preference could not be opened.");
        if (state == "missing")
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
        else if (int.TryParse(state, out var value))
        {
            key.SetValue(valueName, value, RegistryValueKind.DWord);
        }
    }

    private ActionResult ApplyDwordPreference(string featureId, string featureName, string category, string path, string valueName, int desiredValue, string risk, string alreadyMessage, string successMessage, string summary)
    {
        var before = ReadDword(path, valueName);
        if (before == desiredValue.ToString())
        {
            return new ActionResult(true, alreadyMessage);
        }

        try
        {
            SetDword(path, valueName, desiredValue);
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), featureId, featureName, category, risk, DateTimeOffset.Now, before, desiredValue.ToString(), true, summary);
            _changeLog.Record(change);
            return new ActionResult(true, successMessage, change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save this preference: {exception.Message}");
        }
    }

    private ActionResult ApplyStringPreference(string featureId, string featureName, string category, string path, string valueName, string desiredValue, string risk, string alreadyMessage, string successMessage, string summary)
    {
        var before = ReadString(path, valueName);
        if (string.Equals(before, desiredValue, StringComparison.OrdinalIgnoreCase))
        {
            return new ActionResult(true, alreadyMessage);
        }

        try
        {
            SetString(path, valueName, desiredValue);
            var change = new ChangeRecord(Guid.NewGuid().ToString("N"), featureId, featureName, category, risk, DateTimeOffset.Now, before, desiredValue, true, summary);
            _changeLog.Record(change);
            return new ActionResult(true, successMessage, change);
        }
        catch (Exception exception)
        {
            return new ActionResult(false, $"Ownly could not save this privacy preference: {exception.Message}");
        }
    }
}
