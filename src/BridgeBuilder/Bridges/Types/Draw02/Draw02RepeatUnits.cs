namespace BridgeBuilder.Bridges;

// Road appearance is bound by the per-LOD SourceUv/Prepared levels.
// Legacy band-composition aliases must not revive retired materials.
internal static partial class MovableRepeatUnits
{
    static partial void Draw02(string samplerKey, ref string? unitKey) => unitKey = null;
}
