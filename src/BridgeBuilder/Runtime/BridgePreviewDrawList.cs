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
        internal Draw(Mesh mesh, Material material, int subMesh, Matrix4x4 transform, MaterialPropertyBlock properties, Bounds? localBounds = null)
        {
            Mesh = mesh;
            Material = material;
            SubMesh = subMesh;
            Transform = transform;
            Properties = properties;
            LocalBounds = localBounds ?? mesh.bounds;
        }
        internal Bounds LocalBounds { get; }
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
    private readonly Dictionary<RenderPrefab, BridgePreviewOpenPose> _openPoses = new();
    private bool _hasBounds;
    internal Bounds Bounds { get; private set; }
    internal Bounds ViewBounds { get; private set; }
    private bool _hasViewBounds;
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

    internal bool AddRenderPrefab(RenderPrefab prefab, Matrix4x4 transform, BridgePreviewOpenPose? pose = null)
    {
        // Unity type-pattern matching accepts wrappers whose native object has
        // already been destroyed. Do not touch name/materials on those wrappers.
        if (prefab == null)
        {
            return false;
        }
        var meshes = _resources.Meshes(prefab);
        if (meshes.Length == 0)
            throw new InvalidOperationException($"Preview render has no loaded geometry: {prefab.name}.");
        var materials = _resources.Materials(prefab);
        var properties = _resources.Properties(prefab);
        pose?.Bind(properties);
        var materialIndex = 0;
        var added = false;
        foreach (var mesh in meshes)
        {
            if (mesh == null) continue;
            var localBounds = mesh.bounds;
            if (pose != null)
            {
                var measured = BridgePreviewSkinBounds.Measure(mesh, pose.Skin,
                    Matrix4x4.Rotate(Quaternion.Inverse(BridgePreviewCamera.ViewRotation)) * transform);
                localBounds = measured.Local;
                IncludeView(measured.View);
            }
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (materialIndex >= materials.Length || materials[materialIndex] == null)
                {
                    throw new InvalidOperationException($"Preview material slot {materialIndex} is missing for {prefab.name}; available={materials.Length}.");
                }
                _draws.Add(new Draw(mesh, materials[materialIndex++], subMesh, transform, properties, localBounds));
                added = true;
            }
            if (pose == null) Encapsulate(mesh.bounds, transform);
            else Encapsulate(localBounds, transform, includeView: false);
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
            var movable = prefab.GetComponent<MoveableBridge>();
            if (!layoutFound && (prefab.m_Meshes?.Length ?? 0) != 0)
                throw new InvalidOperationException($"Preview object layout is invalid: {prefab.name}.");
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
                    BridgePreviewOpenPose? pose = null;
                    if (movable != null && movable.active &&
                        mesh.TryGet<ProceduralAnimationProperties>(out var animation) && animation.active)
                    {
                        if (!_openPoses.TryGetValue(mesh, out pose))
                        {
                            // Native waterway signal group uses the second lift target.
                            pose = new BridgePreviewOpenPose(animation,
                                local.inverse.MultiplyPoint3x4(new Vector3(0f, movable.m_LiftOffsets.y, 0f)));
                            _openPoses.Add(mesh, pose);
                        }
                        _resources.EnableProceduralAnimation(mesh);
                    }
                    if (!AddRenderPrefab(mesh, transform * local, pose)) return false;
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
                    if (movable != null && movable.active)
                        local = AttachmentPose(prefab, child.m_ParentMesh) * local;
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

    private Matrix4x4 AttachmentPose(ObjectGeometryPrefab prefab, int connection)
    {
        var parts = prefab.m_Meshes ?? Array.Empty<ObjectMeshInfo>();
        if (connection <= 0 || connection < parts.Length) return Matrix4x4.identity;
        // RenderingUtils.FindBoneIndex increments the connection code per mesh slot.
        foreach (var part in parts)
        {
            if (part?.m_Mesh is RenderPrefab render && _openPoses.TryGetValue(render, out var pose))
            {
                var bones = part.m_Mesh.GetComponent<ProceduralAnimationProperties>().m_Bones;
                for (var i = 0; i < bones.Length; i++)
                    if (bones[i].m_ConnectionID == connection)
                    {
                        var local = Matrix4x4.TRS(part.m_Position, part.m_Rotation, Vector3.one);
                        return local * pose.Delta[i] * local.inverse;
                    }
            }
            connection++;
        }
        throw new InvalidOperationException("Movable preview attachment has no matching bone connection.");
    }

    private void Encapsulate(Bounds local, Matrix4x4 transform, bool includeView = true)
    {
        // Bounds are used only for camera framing, never to infer bridge transforms.
        for (var x = -1; x <= 1; x += 2)
        for (var y = -1; y <= 1; y += 2)
        for (var z = -1; z <= 1; z += 2)
        {
            var point = transform.MultiplyPoint3x4(local.center +
                Vector3.Scale(local.extents, new Vector3(x, y, z)));
            if (includeView) IncludeView(new Bounds(
                Quaternion.Inverse(BridgePreviewCamera.ViewRotation) * point, Vector3.zero));
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

    private void IncludeView(Bounds value)
    {
        if (!_hasViewBounds) { ViewBounds = value; _hasViewBounds = true; }
        else { var bounds = ViewBounds; bounds.Encapsulate(value); ViewBounds = bounds; }
    }

    public void Dispose()
    {
        _draws.Clear();
        _objectPath.Clear();
        _objectLayouts.Clear();
        foreach (var pose in _openPoses.Values) pose.Dispose();
        _openPoses.Clear();
        _network.Dispose();
        _lanes.Dispose();
        _resources.Dispose();
    }
}
