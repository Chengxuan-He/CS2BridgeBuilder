using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Complete offline candidate graph. Filtering quality remains unaccepted;
// candidate integration is not deployment or visual acceptance.
internal static class Lift05StructuralMaterial
{
    internal static SurfaceAsset Resolve() => BridgePreparedMaterials.RepeatUnit("Lift05", "85057d5f0c21ff93c646cdc60fa02affe454e15a39b7cca0c899aa1aa861ffe1");
    internal static Vector2 ContentMin => new(Numeric_Types_Lift05_Lift05StructuralMaterial_cs.Values.F32[0], Numeric_Types_Lift05_Lift05StructuralMaterial_cs.Values.F32[1]);
    internal static Vector2 ContentMax => new(Numeric_Types_Lift05_Lift05StructuralMaterial_cs.Values.F32[2], Numeric_Types_Lift05_Lift05StructuralMaterial_cs.Values.F32[3]);
}

// Embedded numerical arrays: Types_Lift05_Lift05StructuralMaterial_cs
internal static class Numeric_Types_Lift05_Lift05StructuralMaterial_cs
{
    static Numeric_Types_Lift05_Lift05StructuralMaterial_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05StructuralMaterial_cs");
}
