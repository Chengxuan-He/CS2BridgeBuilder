using System;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkLanes
{
    // Native m_SectionIndex counts only selected roots that emit pieces, in composition
    // traversal order. Recover original root occurrences by replaying native selection
    // on one root at a time; do not interpret that compact index as a source array index.
    internal int[] SectionRoots(Entity composition)
    {
        RequirePrivateWorld();
        var prefab = EntityManager.GetComponentData<PrefabRef>(composition).m_Prefab;
        var flags = EntityManager.GetComponentData<NetCompositionData>(composition).m_Flags;
        var sections = EntityManager.GetBuffer<NetGeometrySection>(prefab, true);
        var pieces = EntityManager.GetBuffer<NetCompositionPiece>(composition, true);
        var subSections = GetBufferLookup<NetSubSection>(true);
        var sectionPieces = GetBufferLookup<NetSectionPiece>(true);
        using var selected = new NativeList<NetCompositionPiece>(Allocator.Temp);
        var result = new int[pieces.Length];
        var cursor = 0;
        var sectionIndex = 0;
        for (var i = 0; i < sections.Length; i++)
        {
            var root = (flags.m_General & CompositionFlags.General.Invert) != 0
                ? sections.Length - 1 - i : i;
            using var input = new NativeArray<NetGeometrySection>(new[] { sections[root] }, Allocator.Temp);
            selected.Clear();
            // The helper itself applies Invert/Flip and selects the first matching
            // subsection chain, including the native alternate piece condition.
            NetCompositionHelpers.GetCompositionPieces(selected, input, flags, subSections, sectionPieces);
            if (selected.Length == 0) continue;
            for (var j = 0; j < selected.Length; j++, cursor++)
            {
                if (cursor >= pieces.Length || pieces[cursor].m_Piece != selected[j].m_Piece ||
                    pieces[cursor].m_SectionIndex != sectionIndex)
                    throw new InvalidOperationException("Native section provenance does not match the initialized composition.");
                result[cursor] = root;
            }
            sectionIndex++;
        }
        if (cursor != pieces.Length)
            throw new InvalidOperationException("Native section provenance did not cover every composition piece.");
        return result;
    }
}
