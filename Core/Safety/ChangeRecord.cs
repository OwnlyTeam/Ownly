using System;

namespace Ownly.Core.Safety;

public sealed record ChangeRecord(
    string Id,
    string FeatureId,
    string FeatureName,
    string Category,
    string Risk,
    DateTimeOffset AppliedAt,
    string BeforeState,
    string AfterState,
    bool Reversible,
    string Summary)
{
    public bool IsRestored { get; init; }
}

public sealed record ActionResult(bool Success, string Message, ChangeRecord? Change = null);
