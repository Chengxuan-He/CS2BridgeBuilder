using System;
using System.Collections.Generic;
using Game.Prefabs;
using Game.Rendering;
using Unity.Mathematics;
using UnityEngine;
using ObjectStack = Game.Objects.Stack;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Explicit draw commands for the isolated preview. Draws are not inserted into
/// the simulation or the main camera's renderer list.
/// </summary>
internal sealed class BridgePreviewDrawList : IDisposable
{
    internal readonly struct Draw
    {
        internal Draw(Mesh mesh, Material material, int subMesh, Matrix4x4 transform, MaterialPropertyBlock properties)
        {
            Mesh = mesh;
            Material = material;
            SubMesh = subMesh;
            Transform = transform;
            Properties = properties;
        }
        internal Mesh Mesh { get; }
        internal Material Material { get; }
        internal int SubMesh { get; }
        internal Matrix4x4 Transform { get; }
        internal MaterialPropertyBlock Properties { get; }
    }

    private readonly BridgePreviewRenderResources _resources;
    private readonly List<Draw> _draws = new();
    private readonly HashSet<ObjectGeometryPrefab> _objectPath = new();
    private readonly Dictionary<ObjectGeometryPrefab, BridgePreviewObjectLayout> _objectLayouts = new();
    private readonly BridgePreviewNetMesh _network = new();
    private readonly BridgePreviewLaneMesh _lanes = new();
    private bool _hasBounds;
    internal Bounds Bounds { get; private set; }
    internal IReadOnlyList<Draw> Draws => _draws;

    internal BridgePreviewDrawList(BridgePreviewSession session)
        => _resources = new BridgePreviewRenderResources(session);

    internal void RefreshMaterialBindings() => _resources.RefreshMaterialBindings();

    internal bool AddLanes(BridgePreviewComposition.Piece[] pieces, NetCompositionData data,
        Matrix4x4 deckTransform, float start, float length)
    {
        using var composition = new BridgePreviewLanes();
        if (!composition.Build(pieces, data, length, out var lanes))
        {
            return false;
        }
        var matrix = deckTransform * Matrix4x4.Translate(new Vector3(0f, 0f, start));
        foreach (var lane in lanes)
            if (!_lanes.Build(lane, composition.LeftHandTraffic, matrix, _resources, _draws)) return false;
        return true;
    }

    internal bool AddNetSpan(BridgePreviewComposition.Piece[] pieces, NetCompositionData data,
        Matrix4x4 deckTransform, float start, float length)
    {
        if (!_network.Build(pieces, data, _resources, length, out var mesh, out var materials, out var properties))
            return false;
        var matrix = deckTransform * Matrix4x4.Translate(new Vector3(0f, 0f, start + length * .5f));
        for (var sub = 0; sub < mesh.subMeshCount; sub++)
            if (mesh.GetIndexCount(sub) != 0)
                _draws.Add(new Draw(mesh, materials[sub], sub, matrix, properties));
        Encapsulate(mesh.bounds, matrix);
        return true;
    }

    internal bool AddRenderPrefab(RenderPrefab prefab, Matrix4x4 transform)
    {
        // Unity type-pattern matching accepts wrappers whose native object has
        // already been destroyed. Do not touch name/materials on those wrappers.
        if (prefab == null)
        {
            return false;
        }
        var meshes = _resources.Meshes(prefab);
        var materials = _resources.Materials(prefab);
        var properties = _resources.Properties(prefab);
        var materialIndex = 0;
        var added = false;
        foreach (var mesh in meshes)
        {
            if (mesh == null) continue;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (materialIndex >= materials.Length || materials[materialIndex] == null)
                {
                    return false;
                }
                _draws.Add(new Draw(mesh, materials[materialIndex++], subMesh, transform, properties));
                added = true;
            }
            Encapsulate(mesh.bounds, transform);
        }
        return added;
    }

    internal bool AddObject(ObjectGeometryPrefab prefab, Matrix4x4 transform, ObjectStack? alignedStack = null,
        float objectElevation = 0f)
    {
        // Reject recursive ownership rather than walking a corrupt graph forever.
        if (!_objectPath.Add(prefab))
        {
            return false;
        }
        try
        {
            // Each first/middle/last mesh is a template for native stack
            // batching, not a complete tower to draw at its authored origin.
            var layoutFound = TryObjectLayout(prefab, out var layout);
            if (!layoutFound && (prefab.m_Meshes?.Length ?? 0) != 0) return false;
            var tileCounts = new int3(1);
            var offsets = float3.zero;
            var scales = new float3(1f);
            if (layoutFound && layout.HasStack)
                BatchDataHelpers.CalculateStackSubMeshData(alignedStack ?? layout.InitialStack(objectElevation),
                    layout.StackData, out tileCounts, out offsets, out scales);
            // Success means that the active object graph was assembled, not
            // that every authored child emitted a mesh. A child whose meshes
            // require an inactive object state legitimately draws nothing.
            // Treating that as failure aborted its entire parent tower (and
            // hence the bridge) when traversing mounted props/decorations.
            foreach (var part in prefab.m_Meshes ?? Array.Empty<ObjectMeshInfo>())
            {
                if (part?.m_Mesh is not RenderPrefab mesh || part.m_RequireState != 0) continue;
                if (mesh.meshCount == 0) continue;
                var flags = BridgePreviewObjectLayout.Flags(mesh);
                var count = flags == SubMeshFlags.IsStackStart ? tileCounts.x :
                    flags == SubMeshFlags.IsStackMiddle ? tileCounts.y :
                    flags == SubMeshFlags.IsStackEnd ? tileCounts.z : 1;
                for (var tile = 0; tile < count; tile++)
                {
                    var position = part.m_Position;
                    var scale = new float3(1f);
                    if (flags != 0 && layoutFound && layout.HasStack)
                        BatchDataHelpers.CalculateStackSubMeshData(layout.StackData, offsets, scales,
                            tile, flags, ref position, ref scale);
                    var local = Matrix4x4.TRS(position, part.m_Rotation, scale);
                    if (!AddRenderPrefab(mesh, transform * local)) return false;
                }
            }
            // Authored children are part of the created object, not independent
            // icons. Follow their local placement; a freshly built bridge has no
            // damaged/destroyed/construction object state.
            if (prefab.TryGet<ObjectSubObjects>(out var children))
            {
                var random = new System.Random(0);
                foreach (var child in children.m_SubObjects ?? Array.Empty<ObjectSubObjectInfo>())
                {
                    if (child?.m_Object is not ObjectGeometryPrefab geometry ||
                        random.Next(100) >= child.m_Probability) continue;
                    // An object without its own mesh can still own visible
                    // grandchildren. Preserve that traversal as the game does.
                    var position = (Vector3)child.m_Position;
                    // Native child initialization inherits the parent's stored
                    // Elevation, NOT its final aligned world height. ParentMesh
                    // is a bone connection code, never a stack tile index.
                    var childElevation = 0f;
                    if (child.m_ParentMesh == -1) position.y = -objectElevation;
                    else childElevation = BridgePreviewObjectLayout.ChildElevation(objectElevation, position.y);
                    var local = Matrix4x4.TRS(position, child.m_Rotation, Vector3.one);
                    if (!AddObject(geometry, transform * local, objectElevation: childElevation))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        finally { _objectPath.Remove(prefab); }
    }

    internal bool TryObjectLayout(ObjectGeometryPrefab prefab, out BridgePreviewObjectLayout layout)
    {
        if (_objectLayouts.TryGetValue(prefab, out layout)) return true;
        if (!BridgePreviewObjectLayout.TryCreate(prefab, out layout)) return false;
        _objectLayouts.Add(prefab, layout);
        return true;
    }

    private void Encapsulate(Bounds local, Matrix4x4 transform)
    {
        // Bounds are used only for camera framing, never to infer bridge transforms.
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
        {
            var point = transform.MultiplyPoint3x4(local.center +
                Vector3.Scale(local.extents, new Vector3(x, y, z)));
            if (!_hasBounds)
            {
                Bounds = new Bounds(point, Vector3.zero);
                _hasBounds = true;
            }
            else
            {
                var bounds = Bounds;
                bounds.Encapsulate(point);
                Bounds = bounds;
            }
        }
    }

    public void Dispose()
    {
        _draws.Clear();
        _objectPath.Clear();
        _objectLayouts.Clear();
        _network.Dispose();
        _lanes.Dispose();
        _resources.Dispose();
    }
}
