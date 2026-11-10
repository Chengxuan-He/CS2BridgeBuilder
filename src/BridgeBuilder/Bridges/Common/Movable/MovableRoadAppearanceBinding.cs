using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// A generation-local observation of the route already selected by Compose. This
// does not classify new geometry, select another road branch or authorize atlas
// extrapolation. All positions/projections use Compose's road-local frame; the
// caller's RoadToObject pose is required when addressing structural vertices.
internal sealed class MovableRoadAppearanceBinding
{
    internal enum Projection { Original, Corresponding, Prepared, SourceUv, SourceAtlasRepeat, Atlas }
    internal readonly MovableRoadGeometry.Triangle Receiver, SourceReceiver;
    internal readonly SurfaceAsset SourceSurface, Surface;
    internal readonly MovableRoadRoute.Kind Route;
    internal readonly MovableRoadAppearance.Type ReceiverType;
    internal readonly bool MeasuredBase, SelectedInput;
    internal readonly uint AppearanceDomain;
    internal readonly Projection ProjectionKind;
    internal readonly MovableExistingRoadSurface.Patch Patch;
    internal readonly Matrix4x4 SourceFrame;
    internal readonly float CompatibleScale;
    internal readonly MovableRoadRoute.Source? SourceIdentity;
    // The Original descriptor route preserves these source coordinates/PBR. This
    // domain never authorizes extrapolating a different prototype/prepared atlas.
    internal readonly MovableRoadRoute.SourceDomain? SourceDomain;
    // These rows describe only the final receiver triangle's plane. A zero
    // transverse gradient is not permission to stretch a sidewall. Repeat axes
    // and finite sampling bounds remain in Patch; Original keeps source modes.
    internal readonly Vector4 U, V;

    internal MovableRoadAppearanceBinding(MovableRoadGeometry.Triangle receiver,
        MovableRoadGeometry.Triangle sourceReceiver, SurfaceAsset source, SurfaceAsset surface,
        MovableRoadRoute.Kind route, MovableRoadAppearance.Type receiverType, bool measuredBase,
        bool selectedInput, uint domain, Projection projection,
        MovableExistingRoadSurface.Patch patch, Matrix4x4 sourceFrame, float compatibleScale,
        MovableRoadRoute.Source? sourceIdentity = null)
    {
        Receiver = receiver; SourceReceiver = sourceReceiver; SourceSurface = source; Surface = surface;
        Route = route; ReceiverType = receiverType; MeasuredBase = measuredBase; SelectedInput = selectedInput;
        AppearanceDomain = domain; ProjectionKind = projection; Patch = patch;
        SourceFrame = sourceFrame; CompatibleScale = compatibleScale;
        SourceIdentity = sourceIdentity;
        SourceDomain = route==MovableRoadRoute.Kind.Original && measuredBase &&
            projection==Projection.Original ? sourceIdentity?.MeasuredDomain(receiverType) : null;
        var projected = MovableSurfaceAtlas.OriginalProjection(receiver);
        U = projected.U; V = projected.V;
    }
}
