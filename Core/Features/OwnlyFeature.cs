namespace Ownly.Core.Features;

public sealed record OwnlyFeature(
    string Id,
    string Name,
    string Category,
    string Description,
    string Detail,
    string Icon,
    string Risk,
    string Availability,
    string Origin,
    bool Reversible,
    bool RequiresAdmin)
{
    public string Status => $"{Risk.ToUpperInvariant()} · {Availability.ToUpperInvariant()}";
}
