using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    internal MovableRoadAssets.Level[] PrepareBoneDecals(IReadOnlyList<MovableRoadAssets.Level> levels,
        params MovablePaintDeck[] decks)
    {
        var result = new MovableRoadAssets.Level[levels.Count];
        for (int i = 0; i < levels.Count; i++)
        {
            var input = levels[i];
            result[i] = new MovableRoadAssets.Level(input.Source, input.Triangles, input.Surfaces,
                input.Partition!, input.RenderBounds, input.RoadToObject, input.Structure, input.Skeleton,
                input.Lanes, input.ClosedWorld, input.AppearancePageSize, input.TexelsPerMeter,
                input.RoadPartitions, input.LanePartitions, input.RoadTrianglePartitions,
                input.ExtraStructure, input.AdditionalStructures, decks);
        }
        return result;
    }
    // Request-owned placement identities. These are not a cross-owner geometry cache.
    private readonly List<(ObjectPrefab Source, Vector3 Position, Quaternion Rotation, int Parent)> _directRoadMeshes = new();
    private readonly List<PreparedRoadPlacements> _preparedRoadPlacements = new();
    private readonly Dictionary<(ObjectPrefab, Vector3, Quaternion, int), MovableNestedRoadObjects.Node> _nestedRoadMeshes = new();
    private readonly Dictionary<MovableNestedRoadObjects.Node, ObjectPrefab> _nestedRoadCarriers = new();

    private sealed class PreparedRoadPlacements
    {
        internal ObjectSubObjectInfo[] Input = Array.Empty<ObjectSubObjectInfo>();
        internal MovableRoadGeometry.Partition[] Domains = Array.Empty<MovableRoadGeometry.Partition>();
        internal bool[] Moving = Array.Empty<bool>();
        internal Pose Frame;
        internal IReadOnlyList<ObjectSubObjectInfo> Placements = Array.Empty<ObjectSubObjectInfo>();
        internal IReadOnlyList<MovableRoadGeometry.Partition> Partitions = Array.Empty<MovableRoadGeometry.Partition>();
        internal MovableNestedRoadObjects.Node[] Hierarchies = Array.Empty<MovableNestedRoadObjects.Node>();
    }

    private PreparedRoadPlacements PrepareRoadPlacements(IReadOnlyList<ObjectSubObjectInfo> placements,
        Pose frame, IReadOnlyList<MovableRoadGeometry.Partition> partitions, ProceduralAnimationProperties skeleton)
    {
        if (placements.Count != partitions.Count)
            throw new ArgumentException("Road attachment partitions must match the placement list.");
        var moving = new bool[skeleton.m_Bones.Length];
        for (int i = 0; i < moving.Length; i++)
            for (int bone = i; bone >= 0; bone = skeleton.m_Bones[bone].parentId)
                moving[i] |= skeleton.m_Bones[bone].m_Type != (BoneType)0;
        foreach (var item in _preparedRoadPlacements)
        {
            if (!item.Frame.position.Equals(frame.position) || !item.Frame.rotation.Equals(frame.rotation) ||
                item.Input.Length != placements.Count || !item.Moving.SequenceEqual(moving)) continue;
            bool same = true;
            for (int i = 0; i < placements.Count && same; i++)
            {
                var a = item.Input[i]; var b = placements[i];
                same = a.m_Object == b.m_Object && a.m_Position.Equals(b.m_Position) &&
                    a.m_Rotation.Equals(b.m_Rotation) && a.m_ParentMesh == b.m_ParentMesh &&
                    a.m_GroupIndex == b.m_GroupIndex && a.m_Probability.Equals(b.m_Probability) &&
                    MovableRoadLodCache.Tree(item.Domains[i], partitions[i]);
            }
            if (same) return item;
        }
        var prepared = new PreparedRoadPlacements
        {
            // Selection output is immutable for this request. Each later style call may
            // rebuild the list wrapper, so compare its values rather than array identity.
            Input = placements.ToArray(), Domains = partitions.ToArray(), Frame = frame, Moving = moving
        };
        MovableRoadObjects.AddEndSupports(ref placements, frame, ref partitions, bone => moving[bone]);
        prepared.Placements = placements; prepared.Partitions = partitions;
        var modes = new int[placements.Count];
        for (int i = 0; i < modes.Length; i++)
        {
            var mount = MovableRoadObjects.MountBone(frame.rotation.normalized * (Vector3)placements[i].m_Position + frame.position, partitions[i], i);
            modes[i] = !mount.HasValue ? -1 : moving[mount.Value] ? 1 : 0;
        }
        prepared.Hierarchies = MovableNestedRoadObjects.Plans(placements, modes);
        _preparedRoadPlacements.Add(prepared);
        return prepared;
    }

    private bool IsDirectRoadMesh(ObjectSubObjectInfo placement) => _directRoadMeshes.Any(item =>
        item.Source == placement.m_Object && item.Position.Equals((Vector3)placement.m_Position) &&
        item.Rotation.Equals((Quaternion)placement.m_Rotation) && item.Parent == placement.m_ParentMesh);

    private ObjectPrefab? NestedRoadCarrier(ObjectSubObjectInfo placement, List<PrefabBase> created)
    {
        if (!_nestedRoadMeshes.TryGetValue((placement.m_Object, placement.m_Position,
            placement.m_Rotation, placement.m_ParentMesh), out var plan) || plan.Children.Length == 0) return null;
        return MovableNestedRoadObjects.Carrier(_bridgeName, plan, created, _nestedRoadCarriers);
    }

    internal static bool CanMergeRender(RenderPrefab render, HashSet<RenderPrefab> visited)
    {
        if (!visited.Add(render)) return true;
        // Every resolved LOD must use the same static visual protocol. Instance
        // color/emissive data and additional generated bases need their own mapping;
        // removing their native consumer is not equivalent to copying vertex data.
        var animation = render.GetComponent<ProceduralAnimationProperties>();
        if (animation != null && ((animation.m_Animations?.Length ?? 0) != 0 ||
            (animation.m_Bones ?? Array.Empty<ProceduralAnimationProperties.BoneInfo>()).Any(b =>
                b.m_Type != (BoneType)0))) return false;
        if (render.Has<BaseProperties>() ||
            render.Has<ColorProperties>() || render.Has<EmissiveProperties>()) return false;
        var lods = render.GetComponent<LodProperties>()?.m_LodMeshes;
        return lods == null || lods.All(lod => lod != null && CanMergeRender(lod, visited));
    }

    // Deterministic nested visuals share the main model assembly. Native selection
    // and the explicit behavioral exceptions retain their original child branches.
    internal MovableRoadAssets.Level[] PrepareDirectRoadMeshes(
        IReadOnlyList<MovableRoadAssets.Level> levels, IReadOnlyList<ObjectSubObjectInfo> placements,
        Pose roadToObject, IReadOnlyList<MovableRoadGeometry.Partition> partitions)
    {
        var prepared = PrepareRoadPlacements(placements, roadToObject, partitions, levels[0].Skeleton!);
        placements = prepared.Placements; partitions = prepared.Partitions;
        var additions = new List<MovableRoadAssets.Supplement>[levels.Count];
        for (int lod = 0; lod < levels.Count; lod++)
            additions[lod] = new List<MovableRoadAssets.Supplement>(levels[lod].AdditionalStructures);
        var rotation = roadToObject.rotation.normalized;
        for (int i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            var hierarchy = prepared.Hierarchies[i];
            if (!hierarchy.Merged) continue;
            var position = rotation * (Vector3)placement.m_Position + roadToObject.position;
            var mount = MovableRoadObjects.MountBone(position, partitions[i], i);
            if (!mount.HasValue) continue;
            var bones = levels[0].Skeleton!.m_Bones;
            // Leave truly stationary objects as native children; only moving visuals
            // require direct skinning. Parent inheritance also makes a static child move.
            bool moving = false;
            for (int bone = mount.Value; bone >= 0; bone = bones[bone].parentId)
                moving |= bones[bone].m_Type != (BoneType)0;
            if (!moving) continue;
            var facing = rotation * (Quaternion)placement.m_Rotation;
            var visuals = MovableNestedRoadObjects.Visuals(hierarchy);
            var staged = new List<MovableRoadAssets.Supplement>[levels.Count];
            for (int lod = 0; lod < levels.Count; lod++)
            {
                staged[lod] = new List<MovableRoadAssets.Supplement>();
                foreach (var visual in visuals)
                for (int part = 0; part < (visual.Source.m_Meshes?.Length ?? 0); part++)
                {
                    var info = visual.Source.m_Meshes[part];
                    var render = (RenderPrefab)info.m_Mesh;
                    var lods = render.GetComponent<LodProperties>()?.m_LodMeshes;
                    if (lod > 0 && lods != null && lods.Length != 0)
                        render = lods[Math.Min(lod - 1, lods.Length - 1)];
                    var transform = Matrix4x4.TRS(position, facing, Vector3.one) * visual.Transform *
                        Matrix4x4.TRS(info.m_Position, info.m_Rotation, Vector3.one);
                    Mesh[]? meshes = null;
                    try
                    {
                        meshes = PrivateGeometryReader.Read(render);
                        if (meshes.Length == 0) throw new System.IO.InvalidDataException("Attachment source geometry could not be read: " + render.name);
                        for (int mesh = 0; mesh < meshes.Length; mesh++)
                        {
                            var plans = MovableNestedRoadObjects.MeshPlans(meshes[mesh], transform,
                                render.GetComponent<ProceduralAnimationProperties>());
                            // One native model/material slot per original submesh; keep the
                            // source mesh's vertex channels and each exact index stream.
                            for (int slot = 0; slot < meshes[mesh].subMeshCount; slot++)
                            {
                                var plan = plans[slot];
                                staged[lod].Add(new MovableRoadAssets.Supplement(render, mesh, plan,
                                    "RoadObject:" + _directRoadMeshes.Count + ":" + visual.Index + ":" + part + ":" + mesh + ":" + slot,
                                    mount.Value, slot));
                            }
                        }
                    }
                    finally { PrivateGeometryReader.Release(meshes); }
                }
            }
            for (int lod = 0; lod < levels.Count; lod++) additions[lod].AddRange(staged[lod]);
            _directRoadMeshes.Add((placement.m_Object, position, facing, bones[mount.Value].m_ConnectionID));
            _nestedRoadMeshes[(placement.m_Object, position, facing, bones[mount.Value].m_ConnectionID)] = hierarchy;
        }
        var result = new MovableRoadAssets.Level[levels.Count];
        for (int lod = 0; lod < levels.Count; lod++)
        {
            var input = levels[lod];
            result[lod] = new MovableRoadAssets.Level(input.Source, input.Triangles, input.Surfaces,
                input.Partition!, input.RenderBounds, input.RoadToObject, input.Structure, input.Skeleton,
                input.Lanes, input.ClosedWorld, input.AppearancePageSize, input.TexelsPerMeter,
                input.RoadPartitions, input.LanePartitions, input.RoadTrianglePartitions,
                input.ExtraStructure, additions[lod], input.Decals);
        }
        return result;
    }
}
