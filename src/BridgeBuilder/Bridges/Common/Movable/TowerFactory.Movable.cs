using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    private readonly Dictionary<RenderPrefab, RenderPrefab[]> _movableAdditionalRenders = new();
    private readonly HashSet<NetSectionPrefab> _expandedMovableSections = new();

    // Native CalculateCompositionData takes the maximum width of pieces within
    // one section. Additional appearance groups therefore occupy the same section,
    // with identical dimensions, offsets and requirements; they never add a strip.
    internal void ExpandMovableNetworkAppearance()
    {
        foreach(var section in _created.OfType<NetSectionPrefab>())
        {
            if(section.m_Pieces==null || _expandedMovableSections.Contains(section))continue;
            var expanded=new List<NetPieceInfo>();
            foreach(var info in section.m_Pieces)
            {
                expanded.Add(info);
                if(!_movableAdditionalRenders.TryGetValue(info.m_Piece,out var renders))continue;
                foreach(var render in renders)
                {
                    var piece=render as NetPiecePrefab ?? throw new InvalidOperationException("Network appearance group lost its native piece type.");
                    var root=info.m_Piece;
                    // Apply final style-authored widths, which are set after render
                    // creation. Never recalculate them from an appearance subset.
                    piece.m_Layer=root.m_Layer;piece.m_Width=root.m_Width;piece.m_Length=root.m_Length;
                    piece.m_HeightRange=root.m_HeightRange;piece.m_WidthOffset=root.m_WidthOffset;
                    piece.m_NodeOffset=root.m_NodeOffset;piece.m_SideConnectionOffset=root.m_SideConnectionOffset;
                    piece.m_SurfaceHeights=root.m_SurfaceHeights;
                    expanded.Add(new NetPieceInfo {m_Piece=piece,m_Offset=info.m_Offset,
                        m_RequireAll=info.m_RequireAll,m_RequireAny=info.m_RequireAny,m_RequireNone=info.m_RequireNone});
                }
            }
            section.m_Pieces=expanded.ToArray();_expandedMovableSections.Add(section);
        }
    }
    internal MovableRoadMaterial CreateMovableRoadMaterial(Colossal.IO.AssetDatabase.SurfaceAsset source)
        => new(_bridgeName, _styleId ?? throw new InvalidOperationException("Movable material requires a bridge style."), source, _previewGeometry);

    // Lighting conversion writes native root-owned effects. Bind only AFTER the
    // final mechanism exists, so these effects are not left on a staging object.
    internal void AttachMovableRoadObjects(StaticObjectPrefab mechanism,
        IReadOnlyList<ObjectSubObjectInfo> placements, Pose roadToObject,
        IReadOnlyList<MovableRoadGeometry.Partition> placementPartitions)
    {
        if (!_created.Contains(mechanism))
            throw new InvalidOperationException("Road attachments require this factory's owned mechanism.");
        var created = new List<PrefabBase>();
        try
        {
            var bound = MovableRoadObjects.Bind(placements, roadToObject,
                placementPartitions, mechanism, _bridgeName, created);
            var objects = mechanism.GetComponent<ObjectSubObjects>() ?? mechanism.AddComponent<ObjectSubObjects>();
            var combined = new List<ObjectSubObjectInfo>(objects.m_SubObjects ?? Array.Empty<ObjectSubObjectInfo>());
            combined.AddRange(bound);
            objects.m_SubObjects = combined.ToArray();
            mechanism.isDirty = true;
        }
        finally
        {
            // A later effect-schema failure may follow successful lamp cloning.
            // Retain partial children for the enclosing preview/save transaction.
            _created.AddRange(created);
        }
    }

    // Section conversion can allocate both wrappers and private object-producing pieces.
    // Keep the complete result in the same save/preview-disposal graph as render assets.
    internal void AdoptMovableSections(IReadOnlyList<PrefabBase> sections)
    {
        foreach (var prefab in sections)
            if (prefab == null || !MovableAssetOwnership.Matches(prefab.name, _bridgeName, _previewGeometry))
                throw new InvalidOperationException("Converted moving-road sections must retain their owning generation identity.");
        _created.AddRange(sections);
    }

    /// <summary>
    /// Assembles a measured mechanism with prepared render graphs and native child placements.
    /// The style supplies its recorded component template; no fixed-bridge tower template or
    /// live archetype is substituted. The result joins the existing owned-prefab save plan.
    /// </summary>
    internal StaticObjectPrefab CreateMovableObject(string name, bool circular,
        IReadOnlyList<ObjectMeshInfo> meshes, IReadOnlyList<ObjectSubObjectInfo> attachments,
        Action<StaticObjectPrefab> applyRecordedComponents)
    {
        name = TowerPrefabNaming.Safe(name);
        if (!MovableAssetOwnership.Matches(name, _bridgeName, _previewGeometry))
            throw new InvalidOperationException("A moving mechanism must retain its owning generation identity.");
        var result = ScriptableObject.CreateInstance<StaticObjectPrefab>();
        try
        {
            result.name = name;
            result.m_Circular = circular;
            result.m_Meshes = new ObjectMeshInfo[meshes.Count];
            for (var i = 0; i < meshes.Count; i++)
            {
                var item = meshes[i];
                result.m_Meshes[i] = new ObjectMeshInfo
                {
                    m_Mesh = item.m_Mesh, m_Position = item.m_Position,
                    m_Rotation = item.m_Rotation, m_RequireState = item.m_RequireState
                };
            }
            applyRecordedComponents(result);
            // Append after all authored meshes so their indices and child connection
            // codes remain stable. Both render graphs animate on this same object.
            var expanded = new List<ObjectMeshInfo>(result.m_Meshes);
            foreach (var item in result.m_Meshes)
                if (item.m_Mesh is RenderPrefab root && _movableAdditionalRenders.TryGetValue(root, out var additional))
                    foreach (var render in additional)
                        expanded.Add(new ObjectMeshInfo { m_Mesh = render, m_Position = item.m_Position,
                            m_Rotation = item.m_Rotation, m_RequireState = item.m_RequireState });
            result.m_Meshes = expanded.ToArray();
            if (attachments.Count != 0)
            {
                var objects = result.GetComponent<ObjectSubObjects>() ?? result.AddComponent<ObjectSubObjects>();
                objects.m_SubObjects = new ObjectSubObjectInfo[attachments.Count];
                for (var i = 0; i < attachments.Count; i++)
                {
                    var item = attachments[i];
                    ValidateMovableParent(result, item.m_ParentMesh);
                    objects.m_SubObjects[i] = new ObjectSubObjectInfo
                    {
                        m_Object = item.m_Object, m_Position = item.m_Position,
                        m_Rotation = item.m_Rotation, m_ParentMesh = item.m_ParentMesh,
                        m_GroupIndex = item.m_GroupIndex, m_Probability = item.m_Probability
                    };
                }
            }
            _created.Add(result);
            return result;
        }
        catch
        {
            foreach (var component in result.components)
                if (component != null) UnityEngine.Object.DestroyImmediate(component);
            UnityEngine.Object.DestroyImmediate(result);
            throw;
        }
    }

    private static void ValidateMovableParent(StaticObjectPrefab owner, int parent)
    {
        // SubObjectSystem uses this remainder; RenderingUtils.FindBoneIndex increments the
        // requested connection ID after each object mesh. Neither value is a bone-array index.
        var connection = parent % 1000;
        if (connection <= 0 || connection < owner.m_Meshes.Length) return;
        for (var mesh = 0; mesh < owner.m_Meshes.Length; mesh++)
        {
            var bones = owner.m_Meshes[mesh].m_Mesh.GetComponent<ProceduralAnimationProperties>()?.m_Bones;
            if (bones != null)
                foreach (var bone in bones)
                    if (bone.m_ConnectionID == connection + mesh) return;
        }
        throw new InvalidOperationException($"'{owner.name}' attachment parent {parent} has no native procedural connection.");
    }

    /// <summary>
    /// Adds a prepared moving-road or fixed-support render graph to the SAME owned-prefab plan as the bridge
    /// structure. The normal generation system saves these nodes, copies external dependencies
    /// and validates manager independence. This does not admit a style without its measured
    /// structure replacement, network mapping and compatible road/marking conversion.
    /// </summary>
    internal RenderPrefab? CreateMovableRenderGraph(string name, IReadOnlyList<MovableRoadAssets.Level> levels)
    {
        if (!MovableRoadAssets.TryCreate(_bridgeName, name,
                _styleId ?? throw new InvalidOperationException("Movable render graph requires a bridge style."), levels, _previewGeometry,
                out var created, out var error))
        {
            _report.Defect($"'{name}': {error}");
            return null;
        }
        _created.AddRange(created);
        if (created.Length > levels.Count)
        {
            if (created.Length % levels.Count != 0)
                throw new InvalidOperationException("Generated appearance groups require complete LOD graphs.");
            var additional = new RenderPrefab[created.Length / levels.Count - 1];
            for (var group = 1; group <= additional.Length; group++)
                additional[group - 1] = created[group * levels.Count];
            _movableAdditionalRenders.Add(created[0], additional);
        }
        return created[0];
    }
}
