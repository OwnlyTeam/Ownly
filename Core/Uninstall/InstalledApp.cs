namespace Ownly.Core.Uninstall;

/// <summary>One entry read from a Windows "Uninstall" registry key.</summary>
public sealed record InstalledApp(
    string DisplayName,
    string Publisher,
    string Version,
    long EstimatedSizeKb,
    string? InstallLocation,
    string? UninstallString,
    string? QuietUninstallString,
    string RegistryHive,
    string RegistryKeyPath,
    bool IsProtected,
    string ProtectionReason)
{
    public string Command => !string.IsNullOrWhiteSpace(QuietUninstallString) ? QuietUninstallString! : UninstallString ?? "";
    public bool CanUninstall => !IsProtected && !string.IsNullOrWhiteSpace(Command);
}
