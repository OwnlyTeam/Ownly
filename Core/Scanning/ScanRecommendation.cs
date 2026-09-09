using System.Collections.Generic;

namespace Ownly.Core.Scanning;

public sealed record ScanRecommendation(
    string Title,
    string Description,
    string Detail,
    string Category);

public sealed record BloatwareCandidate(
    string DisplayName,
    string Source,
    string Publisher,
    string? UninstallCommand,
    bool IsProtected,
    string ProtectionReason);

public sealed record StartupEntry(string Name, string FullPath);

public sealed record ScanReport(
    IReadOnlyList<ScanRecommendation> Recommendations,
    long TemporaryBytes,
    int StartupItems,
    IReadOnlyList<BloatwareCandidate> BloatwareCandidates,
    IReadOnlyList<StartupEntry> StartupEntries);
