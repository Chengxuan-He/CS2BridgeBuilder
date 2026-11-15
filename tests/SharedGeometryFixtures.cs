using System;
using System.Linq;
namespace BridgeBuilder.Bridges;

// Offline regression-only geometry and measured-data helpers. Not compiled into the mod.
internal static partial class BridgeCables
{
    internal static float TowerOutsideCables(int part, int parts) => Suspension.For(part, parts);
    internal static float NarrowestDeckFor(string towerName, float authoredRoad)
    {
        var cables = For(towerName);
        if (cables == null || cables.Value.Width <= Numeric_BridgeCables_cs.Values.F32[33])
            return Numeric_BridgeCables_cs.Values.F32[34];
        // Losing the whole of it leaves nothing; a metre of margin keeps the result a structure rather
        // than a seam.
        return authoredRoad - cables.Value.Width + Numeric_BridgeCables_cs.Values.F32[35];
    }
}

internal static partial class BridgeTowerSpec
{
    internal static int StackOrderOf(int index, int count) => index <= Numeric_BridgeTowerSpec_cs.Values.I32[16] ? StackOrderFirst : index >= count - Numeric_BridgeTowerSpec_cs.Values.I32[17] ? StackOrderLast : StackOrderMiddle;
}

internal static partial class TowerWidening { internal sealed partial class Profile { internal float OuterThicknessAt(float y) => _outer[Band(y)]; } }
internal static partial class BridgeCables
{
    internal static float Margin => Numeric_BridgeCables_cs.Values.F32[19];
    internal static int PieceFlagDisableTiling => Numeric_BridgeCables_cs.Values.I32[0];
    internal static int PieceFlagPreserveShape => Numeric_BridgeCables_cs.Values.I32[1];
    internal static int PieceFlagBlockTraffic => Numeric_BridgeCables_cs.Values.I32[2];
    internal static int PieceFlagBlockCrosswalk => Numeric_BridgeCables_cs.Values.I32[3];
    internal static int PieceFlagSurface => Numeric_BridgeCables_cs.Values.I32[4];
    internal static int PieceFlagLowerBottomToTerrain => Numeric_BridgeCables_cs.Values.I32[5];
}

internal static partial class BridgeTowerSpec
{
    internal static int PillarTypeNone => -Numeric_BridgeTowerSpec_cs.Values.I32[1];
    internal static int PillarTypeVertical => Numeric_BridgeTowerSpec_cs.Values.I32[2];
    internal static int PillarTypeHorizontal => Numeric_BridgeTowerSpec_cs.Values.I32[3];
    internal static int PillarTypeBase => Numeric_BridgeTowerSpec_cs.Values.I32[4];
    internal static int UiPriority => Numeric_BridgeTowerSpec_cs.Values.I32[6];
    internal static int PlaceholderParts => Numeric_BridgeTowerSpec_cs.Values.I32[7];
    internal static int ReplacementParts => Numeric_BridgeTowerSpec_cs.Values.I32[8];
    internal static int StackDirectionUp => Numeric_BridgeTowerSpec_cs.Values.I32[9];
    internal static int StackDirectionNone => Numeric_BridgeTowerSpec_cs.Values.I32[10];
    internal static int StackDirectionRight => Numeric_BridgeTowerSpec_cs.Values.I32[11];
    internal static int StackDirectionForward => Numeric_BridgeTowerSpec_cs.Values.I32[12];
    internal static int StackOrderFirst => Numeric_BridgeTowerSpec_cs.Values.I32[13];
    internal static int StackOrderMiddle => Numeric_BridgeTowerSpec_cs.Values.I32[14];
    internal static int StackOrderLast => Numeric_BridgeTowerSpec_cs.Values.I32[15];
    internal static float StackStartOverlap => Numeric_BridgeTowerSpec_cs.Values.F32[3];
    internal static float StackEndOverlap => Numeric_BridgeTowerSpec_cs.Values.F32[4];
    internal const bool StackForbidScaling = false;
    internal const string BaseMeshName = "Default_Base Mesh";
    internal const bool BaseUseMinBounds = true;
    internal static int MeshUiPriority => Numeric_BridgeTowerSpec_cs.Values.I32[19];
}
