using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
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
        if (!MovableRoadAssets.TryCreate(_bridgeName, name, levels, _previewGeometry,
                out var created, out var error))
        {
            _report.Defect($"'{name}': {error}");
            return null;
        }
        _created.AddRange(created);
        return created[0];
    }
}
