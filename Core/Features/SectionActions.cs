using System;
using System.Linq;
using Ownly.Core.Safety;

namespace Ownly.Core.Features;

/// <summary>
/// Bridges a section item id to the matching <see cref="SafetyEngine"/> call, and reports whether
/// that change is currently applied (by looking for a non-restored entry in the change log).
/// Every change here is per-user and reversible from the Changes section.
/// </summary>
public static class SectionActions
{
    public static bool IsApplied(string featureId)
    {
        return new ChangeLogService().Read()
            .Any(c => string.Equals(c.FeatureId, featureId, StringComparison.OrdinalIgnoreCase) && !c.IsRestored);
    }

    public static ActionResult Apply(string featureId)
    {
        var engine = new SafetyEngine();
        return featureId switch
        {
            "customize.theme" => engine.ApplyDarkMode(),
            "customize.compact-menu" => engine.ApplyClassicContextMenu(),
            "customize.taskbar" => engine.ApplyLeftTaskbarAlignment(),
            "customize.file-extensions" => engine.ApplyShowFileExtensions(),
            "customize.hidden-files" => engine.ApplyShowHiddenFiles(),
            "customize.explorer-home" => engine.ApplyExplorerToThisPc(),
            "customize.notifications" => engine.ApplyDisableNotifications(),
            "clean.windows-suggestions" => engine.ApplyDisableWindowsSuggestions(),
            "clean.background-apps" => engine.ApplyLimitBackgroundApps(),
            "clean.temp" => engine.QuarantineTemporaryFiles(),
            "optimize.visual-effects" => engine.ApplyBestPerformanceVisuals(),
            "optimize.game-mode" => engine.ApplyGameMode(),
            "privacy.advertising-id" => engine.ApplyDisableAdvertisingId(),
            "privacy.tailored" => engine.ApplyDisableTailoredExperiences(),
            "privacy.typing" => engine.ApplyLimitTypingPersonalization(),
            "privacy.activity-history" => engine.ApplyDisableActivityHistory(),
            "privacy.cloud-search" => engine.ApplyDisableCloudSearch(),
            "privacy.camera" => engine.ApplyLimitCamera(),
            "privacy.microphone" => engine.ApplyLimitMicrophone(),
            "privacy.location" => engine.ApplyLimitLocation(),
            _ => new ActionResult(false, "That action is not available in this build.")
        };
    }

    public static ActionResult Revert(string featureId)
    {
        var change = new ChangeLogService().Read()
            .Where(c => string.Equals(c.FeatureId, featureId, StringComparison.OrdinalIgnoreCase) && !c.IsRestored)
            .OrderByDescending(c => c.AppliedAt)
            .FirstOrDefault();

        return change is null
            ? new ActionResult(false, "Ownly has no record of this change to restore.")
            : new SafetyEngine().Restore(change);
    }
}
