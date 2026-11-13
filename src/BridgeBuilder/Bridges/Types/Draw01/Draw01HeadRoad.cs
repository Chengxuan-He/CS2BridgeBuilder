using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw01HeadRoad
{
    internal static NetSectionInfo[] Apply(NetGeometryPrefab target, TowerFactory factory, NetSectionInfo[] sections, List<PrefabBase> created)
    {
        var mappings = new Dictionary<string, (NetPiecePrefab Source, Vector4 U, float Tangent, CompositionFlags RoadFlags, CompositionFlags HeadFlags)>();
        var headPaths = new HashSet<string>();
        // Use the same conditional-root traversal as replacement. Returning each
        // original piece makes this a read-only inventory, without section clones.
        Draw01HeadSections.Apply(target.name, sections, (path, root, piece) =>
        {
            headPaths.Add(path);
            return piece.m_Piece;
        }, created);
        var previous = target.m_Sections;
        try
        {
            target.m_Sections = sections;
            var context = BridgeRoadGeneration.Find(target)
                ?? throw new InvalidOperationException("Draw01 head requires the common elevated road context.");
            var flags = context.Flags;
            flags.m_General &= ~CompositionFlags.General.Node;
            flags.m_General |= CompositionFlags.General.Edge | CompositionFlags.General.FixedNodeSize;
            using var native = new BridgePreviewComposition(trackOccurrences: true);
            foreach (var occurrence in native.Compose(target, flags, out _, includeOverhead: false))
            {
                if (!headPaths.Contains(occurrence.SourcePath))
                    continue;
                if (!Sources(occurrence.Prefab).Any(source => Draw01HeadAsphaltFaces.TryGet(source, out _)))
                    continue;
                float direction = (occurrence.Composition.m_SectionFlags & NetSectionFlags.Invert) != (Game.Prefabs.NetSectionFlags)(Numeric_Types_Draw01_Draw01HeadRoad_cs.Values.I32[0]) ? Numeric_Types_Draw01_Draw01HeadRoad_cs.Values.I32[1] : -Numeric_Types_Draw01_Draw01HeadRoad_cs.Values.I32[2];
                var u = Draw01AsphaltRepeat.NativeU(direction, occurrence.Composition.m_Offset.x);
                // Every measured native source asphalt U decreases with source X.
                // Reverse the original tangent iff the new source-U derivative changes sign.
                var entry = (occurrence.Prefab, u, -direction, context.Flags, flags);
                mappings.Add(occurrence.SourcePath, entry);
            }
        }
        finally
        {
            target.m_Sections = previous;
        }

        var replacements = new Dictionary<string, NetPiecePrefab>();
        return Draw01HeadSections.Apply(target.name, sections, (path, root, piece) =>
        {
            if (!mappings.TryGetValue(path, out var projection))
                return piece.m_Piece;
            if (replacements.TryGetValue(path, out var cached))
                return cached;
            var sources = Sources(piece.m_Piece).ToArray();
            var levels = new MovableRoadAssets.Level[sources.Length];
            var surface = BridgePreparedMaterials.RepeatUnit("Draw01", "3372e1ba2f8e17b7b63f9409850bebc33e5da71ccb2450749f1e950bec628518");
            for (int lod = Numeric_Types_Draw01_Draw01HeadRoad_cs.Values.I32[3]; lod < sources.Length; lod++)
            {
                if (!Draw01HeadAsphaltFaces.TryGet(sources[lod], out var faces))
                {
                    levels[lod] = MovableRoadAssets.Level.NativeOriginal(sources[lod], sources[lod].bounds);
                    continue;
                }
                var mapping = new MovableNetworkRoadMapping(surface, new Dictionary<int, int[]> { [Numeric_Types_Draw01_Draw01HeadRoad_cs.Values.I32[4]] = faces }, projection.U, Draw01AsphaltRepeat.LongitudinalDensity, projection.Tangent);
                levels[lod] = new MovableRoadAssets.Level(sources[lod], sources[lod].bounds, mapping);
            }

            var replacement = factory.CreateMovableRenderGraph(target.name + " Head Road " + replacements.Count, levels) as NetPiecePrefab ?? throw new InvalidOperationException("Draw01 native head render graph creation failed.");
            replacements.Add(path, replacement);
            return replacement;
        }, created);
    }

    private static IEnumerable<RenderPrefab> Sources(NetPiecePrefab piece)
        => new[] { (RenderPrefab)piece }.Concat(
            piece.GetComponent<LodProperties>()?.m_LodMeshes ?? Array.Empty<RenderPrefab>());

}

// Embedded numerical arrays: Types_Draw01_Draw01HeadRoad_cs
internal static class Numeric_Types_Draw01_Draw01HeadRoad_cs
{
    static Numeric_Types_Draw01_Draw01HeadRoad_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01HeadRoad_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01HeadRoad_cs"));
}
