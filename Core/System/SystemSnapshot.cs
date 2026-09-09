namespace Ownly.Core.System;

/// <summary>
/// Read-only information about the current Windows device.
/// This model intentionally contains no mutation or remediation commands.
/// </summary>
public sealed record SystemSnapshot(
    string DeviceName,
    string WindowsVersion,
    string Processor,
    string Memory,
    string Storage,
    string StorageDetail);
