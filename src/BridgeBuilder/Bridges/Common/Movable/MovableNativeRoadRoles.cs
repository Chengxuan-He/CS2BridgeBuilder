using BridgeBuilder.Runtime;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

// Source-face measurements, not runtime geometric classification. Geometry identity
// permits differently named section/piece wrappers to share the same inspected source.
// This table is intentionally explicit: unknown geometry has no inferred default role.
internal static class MovableNativeRoadRoles
{
    internal enum Role { Carriageway, Sidewalk }

    // Exact native producer records from resources.assets (game 1.6.2f1).
    // Geometry alone cannot identify object rules: wrappers sharing a mesh can
    // place different furniture. Names below are complete measured identities,
    // not substring/type inference from a selected object's display name.
    internal static bool TryGetAttachment(MovableRoadObjects.Rule source, out Role role)
    {
        role = default;
        var geometry = source.SourcePiece.geometryAsset?.id.guid.ToString();
        switch ((source.SourcePiece.name, source.SourceRuleIndex, source.Prefab.name))
        {
            case ("Car Drive Piece 3", 0, "RoadArrow Placeholder"):
            case ("Car Drive Piece 3", 1, "LaneMarkings Placeholder"):
            case ("Car Drive Piece 3", 2, "PathMarkings Placeholder"):
                return geometry == "65b7e0cbd9c16168c9f6c306369b88f6";
            // Median Piece 2 owns the inspected double-light producer (rule 0).
            // The divider and its furniture travel with the carriageway, not the
            // outside pedestrian strip, despite sharing the pavement's height.
            case ("Median Piece 2", 0, "Street Light Double Placeholder"):
                return geometry == "61ff20f7140db6ddb6fe95db7533d170";
            // Ordinary Small Road uses the same inspected pavement geometry, but
            // its tram-pole rules shift the two streetlight producer indices.
            case ("Sidewalk Piece 3.5", 0, "Traffic Sign Placeholder"):
            case ("Sidewalk Piece 3.5", 4, "Street Light Single Placeholder"):
            case ("Sidewalk Piece 3.5", 5, "Street Light Single Placeholder"):
            case ("Sidewalk Piece 3.5 - NoTram", 0, "Traffic Sign Placeholder"):
            case ("Sidewalk Piece 3.5 - NoTram", 2, "Street Light Single Placeholder"):
            case ("Sidewalk Piece 3.5 - NoTram", 3, "Street Light Single Placeholder"):
                role = Role.Sidewalk;
                return geometry == "49670fd1cafa11667cbd47d1c380fd04";
            // Rule 1 is a network-owned crossing signal. Rules 4/5 are tunnel
            // lighting and ground-only props, not measured moving attachments.
            // Native eligibility/selection happens before this lookup; never
            // silently discard a surviving object with no measured mapping.
            default:
                return false;
        }
    }

    internal static bool TryGet(NetPiecePrefab piece, BridgePreviewNetMesh.SourceFace face, out Role role)
    {
        role = default;
        if (piece.geometryAsset == null || face.Mesh != 0 || face.Submesh != 0 || face.Triangle < 0)
            return false;
        switch (piece.geometryAsset.id.guid.ToString())
        {
            // CarDriveLane3; 36 vertices, 32 triangles; native game 1.6.2f1.
            // SHA256 82c251060320f6c67062151a3893c48f6c69f8e1bcda842637590267407830d8
            case "65b7e0cbd9c16168c9f6c306369b88f6":
                return face.Triangle < 32;
            // RoadIntersectionMiddle3; 53 vertices, 63 triangles.
            // SHA256 16c76d83d4cd8843e432e31618800d1f1bc3b81a8487e1d98b0dcc10b24469c9
            case "92eb25687f4a6fcd79cf82a761ee2e23":
                return face.Triangle < 63;
            // RoadIntersectionMiddle2, used by the zero-allocation-width median.
            // SHA256 f16004ed31c3ed5be92a8076dd6183ca5c2a964eb4c098ff594fa49afbbef34f
            case "dac11ed9cbc727bc8dcb1c6982d81ca4":
                return face.Triangle < 63;
            // Median2; 252 vertices, 224 triangles; native NetDividerPiece.
            // Both its raised divider and lower margins belong to the central
            // carriageway assembly. Height alone does not make it a sidewalk.
            // SHA256 7377a7dddebfeb6113303793279e42be9ae31b01278912641b970760b0047a93
            case "61ff20f7140db6ddb6fe95db7533d170":
                return face.Triangle < 224;
            // Sidewalk3.5; 144 vertices, 128 triangles. The lower road strip is
            // distinct from the raised pavement, curb top and vertical curb faces.
            // SHA256 e5dc589b5e08487ffbf5790a6f8ad3d65bb534801f0039c4f4021aacb856e46b
            case "49670fd1cafa11667cbd47d1c380fd04":
                if (face.Triangle >= 128) return false;
                role = (face.Triangle >= 80 && face.Triangle < 96) || face.Triangle >= 112
                    ? Role.Carriageway : Role.Sidewalk;
                return true;
            default:
                return false;
        }
    }
}
