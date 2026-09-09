using System.Collections.Generic;
using System.Linq;

namespace Ownly.Core.Features;

/// <summary>
/// The owned catalog of ideas Ownly can research and implement safely over time.
/// Catalog entries are descriptions only; they do not execute system changes.
/// </summary>
public static class FeatureCatalog
{
    public static IReadOnlyList<OwnlyFeature> All { get; } = new List<OwnlyFeature>
    {
        // CLEAN — familiar cleanup ideas
        F("clean.windows-suggestions", "Limit Windows suggestions", "Clean", "Reduce optional tips, recommendations, and promotional surfaces in Windows.", "A reversible per-user preference; it does not remove apps or Windows components", "\uE72E", "Low", "Available", "Windows option", true, false),
        F("clean.background-apps", "Limit background apps", "Clean", "Limit background activity for apps that do not need to run continuously.", "This is broader than a single app and is marked Moderate risk", "\uE7BA", "Moderate", "Available", "Windows option", true, false),
        F("clean.temp", "Temporary files", "Clean", "Review temporary files created by Windows and apps.", "Ownly moves eligible files to a private backup so the cleanup can be restored", "\uE74D", "Low", "Available", "Windows option", true, false),
        F("clean.delivery-cache", "Delivery Optimization cache", "Clean", "Review cached update files shared by Windows Update.", "Can free space without touching installed updates", "\uE8B7", "Low", "Research", "Lesser-known", true, true),
        F("clean.thumbnail-cache", "Thumbnail cache", "Clean", "Review generated image previews stored by File Explorer.", "Windows recreates thumbnails when needed", "\uE8B7", "Low", "Planned", "Windows option", true, false),
        F("clean.browser-cache", "Browser cache review", "Clean", "See browser cache sizes before deciding what to clear.", "Ownly will never remove browser data without explicit approval", "\uE774", "Low", "Planned", "Ownly idea", true, false),
        F("clean.recycle-bin", "Recycle Bin review", "Clean", "Review items waiting in the Recycle Bin.", "Permanent emptying always requires a separate confirmation", "\uE74D", "Moderate", "Planned", "Windows option", true, false),
        F("clean.old-updates", "Old update cleanup", "Clean", "Identify superseded Windows update components.", "Requires Windows servicing checks and a restore-aware flow", "\uE7F4", "Moderate", "Research", "Lesser-known", true, true),
        F("clean.large-files", "Large file finder", "Clean", "Find unusually large files without deleting anything.", "A review-first storage explorer for your own files", "\uEDA2", "Low", "Planned", "Ownly idea", true, false),
        F("clean.unused-apps", "Unused app review", "Clean", "Highlight installed applications that may no longer be needed.", "Ownly will never silently uninstall an app", "\uE7B8", "Moderate", "Planned", "Windows option", true, true),
        F("clean.old-downloads", "Downloads review", "Clean", "Review older files in Downloads by age and size.", "Personal files remain untouched until you choose an action", "\uE8B7", "Low", "Planned", "Ownly idea", true, false),
        F("clean.log-review", "Log file review", "Clean", "Find verbose application logs and show what created them.", "Keeps diagnostics understandable before any cleanup", "\uE90F", "Low", "Research", "Lesser-known", true, false),

        // CUSTOMIZE — familiar controls plus deeper Windows options
        F("customize.theme", "Theme and appearance", "Customize", "Choose light, dark, or system-aware presentation.", "Ownly can apply dark mode per user and restore the prior theme", "\uE70F", "Low", "Available", "Windows option", true, false),
        F("customize.taskbar", "Taskbar layout", "Customize", "Explore taskbar alignment, visibility, and behavior.", "Ownly can apply a reversible left-alignment preference where Windows supports it", "\uE7F4", "Low", "Available", "Windows option", true, false),
        F("customize.taskbar-labels", "Taskbar labels", "Customize", "Review the compact or combined taskbar button style.", "A lesser-known taskbar preference", "\uE7F4", "Low", "Research", "Lesser-known", true, false),
        F("customize.start", "Start menu", "Customize", "Tune recommendations, layout, and shortcut behavior.", "Each option explains what Windows may show differently", "\uE8A5", "Low", "Planned", "Windows option", true, false),
        F("customize.file-extensions", "File extensions", "Customize", "Show file extensions in File Explorer.", "A small setting with a big safety benefit", "\uE8B7", "Low", "Available", "Windows option", true, false),
        F("customize.hidden-files", "Hidden files", "Customize", "Choose whether hidden files are visible in Explorer.", "A reversible Explorer preference; protected system files stay separately controlled", "\uE8B7", "Low", "Available", "Windows option", true, false),
        F("customize.compact-menu", "Classic context menu", "Customize", "Use the full context menu without the extra Windows 11 click.", "Ownly creates only the known per-user override and can remove it on restore", "\uE74D", "Low", "Available", "Lesser-known", true, false),
        F("customize.notifications", "Notifications", "Customize", "Review notification priority and suggestion surfaces.", "Ownly can limit Windows toast notifications for this user and restore the previous setting", "\uE7E7", "Low", "Available", "Windows option", true, false),
        F("customize.explorer-home", "Explorer start location", "Customize", "Choose Home, This PC, or a custom folder as Explorer opens.", "Ownly can set File Explorer to open at This PC and restore the previous location", "\uE8B7", "Low", "Available", "Lesser-known", true, false),
        F("customize.ownly-presets", "Ownly appearance presets", "Customize", "Save a complete Ownly preference profile for different situations.", "One of Ownly's signature ideas", "\uE70F", "Low", "Planned", "Ownly idea", true, false),

        // OPTIMIZE — performance and behavior
        F("optimize.startup", "Startup review", "Optimize", "See programs configured to start with Windows.", "Review first; personal Startup folder entries can be disabled and restored", "\uE81C", "Low", "Available", "Windows option", true, false),
        F("optimize.power-mode", "Power mode", "Optimize", "Choose a Windows power mode with its tradeoffs explained.", "Balanced, best performance, and efficiency can each be restored", "\uE7E8", "Moderate", "Available", "Windows option", true, false),
        F("optimize.background-apps", "Background activity", "Optimize", "Review applications that may run in the background.", "Shows battery and privacy implications", "\uE7BA", "Moderate", "Research", "Lesser-known", true, false),
        F("optimize.visual-effects", "Visual effects", "Optimize", "Use a Best Performance visual-effects profile.", "Useful on lower-powered hardware and fully reversible", "\uE7F4", "Low", "Available", "Windows option", true, false),
        F("optimize.game-mode", "Game Mode", "Optimize", "Enable the Windows Game Mode preference for this user.", "Ownly will not promise FPS gains without measurement", "\uE7FC", "Low", "Available", "Windows option", true, false),
        F("optimize.gpu-scheduling", "Hardware GPU scheduling", "Optimize", "Check whether hardware-accelerated GPU scheduling is available.", "Hardware and driver support varies", "\uE950", "Moderate", "Research", "Lesser-known", true, true),
        F("optimize.storage-sense", "Storage Sense", "Optimize", "Review automatic storage management rules.", "Windows can clean more than expected if configured carelessly", "\uEDA2", "Moderate", "Research", "Windows option", true, false),
        F("optimize.network-adapter", "Network adapter overview", "Optimize", "Show adapter, link, and DNS information.", "Read-only diagnostics before any network reset", "\uE774", "Low", "Planned", "Ownly idea", true, false),
        F("optimize.memory-health", "Memory health", "Optimize", "Show installed memory and current availability.", "A clear snapshot rather than a vague score", "\uE950", "Low", "Preview", "Ownly idea", true, false),
        F("optimize.focus-profile", "Focus profile", "Optimize", "Create a reversible work or gaming focus profile.", "Ownly can group safe preferences into one reviewable bundle", "\uE7FC", "Moderate", "Planned", "Ownly idea", true, false),

        // PRIVACY — controls and explanations
        F("privacy.advertising-id", "Advertising ID", "Privacy", "Review Windows advertising personalization.", "Ownly can limit the per-user advertising ID and restore the prior value", "\uE7BA", "Low", "Available", "Windows option", true, false),
        F("privacy.diagnostics", "Diagnostic data", "Privacy", "Understand optional diagnostic and feedback levels.", "Windows editions expose different choices", "\uE72E", "Moderate", "Research", "Windows option", true, false),
        F("privacy.activity-history", "Activity history", "Privacy", "Limit Windows activity history publishing and sync.", "A lesser-known privacy surface with a reversible per-user control", "\uE81C", "Moderate", "Available", "Lesser-known", true, false),
        F("privacy.location", "Location access", "Privacy", "Limit location access for this user.", "Apps that need location will need permission again after restore", "\uE774", "Moderate", "Available", "Windows option", true, false),
        F("privacy.camera-mic", "Camera and microphone", "Privacy", "Review access permissions for camera and microphone.", "No permission is changed by the scanner", "\uE720", "Moderate", "Planned", "Windows option", true, false),
        F("privacy.camera", "Camera access", "Privacy", "Limit camera access for this user.", "Apps that need the camera will need permission again after restore", "\uE720", "Moderate", "Available", "Windows option", true, false),
        F("privacy.microphone", "Microphone access", "Privacy", "Limit microphone access for this user.", "Apps that need the microphone will need permission again after restore", "\uE720", "Moderate", "Available", "Windows option", true, false),
        F("privacy.typing", "Typing personalization", "Privacy", "Understand optional typing and inking personalization.", "Ownly can limit implicit typing and inking collection for this user", "\uE70F", "Low", "Available", "Lesser-known", true, false),
        F("privacy.cloud-search", "Cloud content search", "Privacy", "Review whether cloud content is included in Windows search.", "Ownly can limit Microsoft account and work account cloud search where Windows supports it", "\uE721", "Moderate", "Available", "Lesser-known", true, false),
        F("privacy.tailored", "Tailored experiences", "Privacy", "Limit personalization based on diagnostic data.", "A small setting that is easy to miss and fully reversible", "\uE72E", "Low", "Available", "Lesser-known", true, false),
        F("privacy.permissions-audit", "Permission audit", "Privacy", "Build a readable overview of app permissions.", "One of Ownly's core transparency features", "\uE716", "Low", "Planned", "Ownly idea", true, false),
        F("privacy.private-profile", "Private-by-default profile", "Privacy", "Bundle privacy preferences into a reviewable profile.", "Every included setting is shown before approval", "\uE72E", "Moderate", "Planned", "Ownly idea", true, false),

        // TOOLS — safe diagnostics and future utilities
        F("tools.system-info", "System information", "Tools", "Hardware, Windows, and storage information in one place.", "Already available on the Overview dashboard", "\uE77B", "Low", "Available", "Ownly idea", true, false),
        F("tools.startup-manager", "Startup manager", "Tools", "Review startup entries with publisher and path details.", "Personal Startup folder entries can be moved to backup and restored", "\uE81C", "Moderate", "Available", "Windows option", true, true),
        F("tools.service-viewer", "Services viewer", "Tools", "Inspect Windows services and their descriptions.", "Changing a service remains an advanced feature", "\uE7B8", "High", "Research", "Lesser-known", true, true),
        F("tools.disk-analyzer", "Disk analyzer", "Tools", "Explore which folders use the most storage.", "Read-only visualization before cleanup", "\uEDA2", "Low", "Planned", "Ownly idea", true, false),
        F("tools.network-info", "Network information", "Tools", "Show adapter, IP, DNS, and gateway details.", "Useful for troubleshooting without resetting anything", "\uE774", "Low", "Planned", "Windows option", true, false),
        F("tools.environment", "Environment variables", "Tools", "Inspect user and system environment variables.", "Editing requires an explicit advanced confirmation", "\uE943", "Moderate", "Research", "Lesser-known", true, true),
        F("tools.hosts-review", "Hosts file review", "Tools", "Read and explain local hosts-file entries.", "Writing to hosts remains locked behind Advanced Mode", "\uE8A5", "High", "Research", "Lesser-known", true, true),
        F("tools.repair-center", "Repair center", "Tools", "Guided SFC, DISM, update, and network diagnostics.", "Each repair command shows expected duration and impact", "\uE90F", "Moderate", "Research", "Windows option", true, true),
        F("tools.restore-point", "Restore point", "Tools", "Create and inspect Windows restore points.", "A safety layer for significant future changes", "\uE72E", "Moderate", "Planned", "Windows option", true, true),
        F("tools.ownly-audit", "Ownly audit report", "Tools", "Export a readable snapshot of your configuration and recommendations.", "A signature Ownly transparency feature", "\uE8A5", "Low", "Planned", "Ownly idea", true, false)
    };

    public static IReadOnlyList<OwnlyFeature> ForCategory(string category) =>
        All.Where(feature => feature.Category.Equals(category, global::System.StringComparison.OrdinalIgnoreCase)).ToList();

    private static OwnlyFeature F(string id, string name, string category, string description, string detail, string icon, string risk, string availability, string origin, bool reversible, bool requiresAdmin) =>
        new(id, name, category, description, detail, icon, risk, availability, origin, reversible, requiresAdmin);
}
