using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw01FixedBase
{
    // The native Bottom layer is an independent structural cross-section, not a
    // sidewalk or traffic layout. Its two outer 3.5 m strips translate rigidly;
    // the two inner 3 m strips retain their common centre seam while widening.
    // Native node pieces close ordinary ends; the mechanism owns fixed-size nodes.
    internal static void Apply(NetGeometryPrefab target, TowerFactory factory, float halfDelta)
    {
        var middle = Section("Middle", Piece(0), Piece(1));
        var edge = Section("Edge", Piece(2), Piece(3));
        var intersection = ScriptableObject.CreateInstance<NetSectionPrefab>();
        intersection.name = target.name + " Fixed Base Intersection";
        factory.AdoptMovableSections(new PrefabBase[] { intersection });
        intersection.m_SubSections = Array.Empty<NetSubSectionInfo>();
        intersection.m_Pieces = new[] { new NetPieceInfo
        {
            m_Piece = Piece(4),
            m_RequireAll = new[] { NetPieceRequirements.Elevated, (NetPieceRequirements)5 },
            m_RequireAny = new[] { NetPieceRequirements.Intersection, (NetPieceRequirements)22 },
            m_RequireNone = Array.Empty<NetPieceRequirements>()
        } };
        var entries = new List<NetSectionInfo>();
        Add(edge, true); Add(middle, true); Add(intersection, false, true);
        Add(middle, false); Add(edge, false);
        var overhead = target.AddOrGetComponent<OverheadNetSections>();
        // Like ordinary bridge generation, replace the selected road's structural
        // overlay. Appending would pack both structures along one overhead cursor.
        overhead.m_Sections = entries.ToArray();
        overhead.active = true;

        NetPiecePrefab Piece(int family)
        {
            var source = factory.ResolveOriginal<NetPiecePrefab>(Draw01FixedBaseData.Names[family]);
            var sources = new[] { (RenderPrefab)source }.Concat(
                source.GetComponent<LodProperties>()?.m_LodMeshes ?? Array.Empty<RenderPrefab>()).ToArray();
            var levels = new MovableRoadAssets.Level[sources.Length];
            for (var lod = 0; lod < sources.Length; lod++)
            {
                Mesh[]? meshes = null;
                try
                {
                    meshes = PrivateGeometryReader.Read(sources[lod]);
                    if (meshes.Length != 1 || meshes[0].subMeshCount != 1)
                        throw new InvalidOperationException("Draw01 fixed base requires its measured single-mesh layout.");
                    var plan = Draw01FixedBaseData.Plan(family, lod, meshes[0].vertices,
                        meshes[0].GetIndices(0), halfDelta);
                    var bounds = sources[lod].bounds;
                    bounds.min.x = plan.Positions.Min(p => p.x);
                    bounds.max.x = plan.Positions.Max(p => p.x);
                    levels[lod] = new MovableRoadAssets.Level(sources[lod], bounds, new[] { plan });
                }
                finally { PrivateGeometryReader.Release(meshes); }
            }
            if (sources.Length != (family == 4 ? 1 : 3))
                throw new InvalidOperationException("Draw01 fixed base is missing a measured LOD.");
            var result = factory.CreateMovableRenderGraph(target.name + " Fixed Base " + family, levels)
                as NetPiecePrefab ?? throw new InvalidOperationException("Draw01 fixed base network-piece creation failed.");
            if (family < 2) result.m_Width = source.m_Width + halfDelta;
            return result;
        }

        NetSectionPrefab Section(string role, NetPiecePrefab run, NetPiecePrefab node)
        {
            var section = ScriptableObject.CreateInstance<NetSectionPrefab>();
            section.name = target.name + " Fixed Base " + role;
            factory.AdoptMovableSections(new PrefabBase[] { section });
            section.m_SubSections = Array.Empty<NetSubSectionInfo>();
            section.m_Pieces = new[]
            {
                new NetPieceInfo { m_Piece = run,
                    m_RequireAll = new[] { NetPieceRequirements.Elevated },
                    m_RequireAny = Array.Empty<NetPieceRequirements>(),
                    m_RequireNone = new[] { NetPieceRequirements.Node } },
                new NetPieceInfo { m_Piece = node,
                    m_RequireAll = new[] { NetPieceRequirements.Elevated, NetPieceRequirements.Node },
                    m_RequireAny = Array.Empty<NetPieceRequirements>(),
                    m_RequireNone = Array.Empty<NetPieceRequirements>() }
            };
            return section;
        }

        void Add(NetSectionPrefab section, bool invert, bool median = false)
        {
            entries.Add(new NetSectionInfo { m_Section = section, m_Invert = invert, m_Median = median,
                m_RequireAll = new[] { NetPieceRequirements.Elevated },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = new[] { NetPieceRequirements.FixedNodeSize } });
            entries.Add(new NetSectionInfo { m_Section = section, m_Invert = invert, m_Median = median,
                m_HalfLength = true,
                m_RequireAll = new[] { NetPieceRequirements.Elevated, NetPieceRequirements.FixedNodeSize },
                m_RequireAny = Array.Empty<NetPieceRequirements>(),
                m_RequireNone = new[] { NetPieceRequirements.Node } });
        }
    }
}
