using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North.Identity;

/// <summary>Registered facade names and shop signs, using the shared frozen scene-text atlas.</summary>
public sealed class IdentitySignsSite : ZoneSite
{
    public override string Id => "20c.identity-signs";

    public override void Build(SiteBuilder b)
    {
        foreach (var record in b.Context.World.Features.Values
                     .Where(f => f.Id.Value.StartsWith(NorthFeatureIds.IdentitySignPrefix, StringComparison.Ordinal))
                     .OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            NorthIdentityGeometry.Sign(b, record);
    }
}
