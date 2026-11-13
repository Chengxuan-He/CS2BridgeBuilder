using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// One immutable hierarchy plan supplies both the skinned visuals and their native
// functional carriers. Selection-dependent branches remain native, not pre-rolled.
internal static class MovableNestedRoadObjects
{
    internal sealed class Node : IEquatable<Node>
    {
        internal readonly ObjectPrefab Source;
        internal readonly bool Merged;
        internal readonly ObjectSubObjectInfo[] Records;
        internal readonly Node[] Children;
        internal Node(ObjectPrefab source, bool merged, ObjectSubObjectInfo[] records, Node[] children)
        { Source = source; Merged = merged; Records = records; Children = children; }
        public bool Equals(Node? other) => other != null && ReferenceEquals(Source, other.Source) &&
            Merged == other.Merged && Children.SequenceEqual(other.Children);
        public override bool Equals(object? other) => other is Node node && Equals(node);
        public override int GetHashCode()
        {
            int hash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Source) * 397 ^ Merged.GetHashCode();
            foreach (var child in Children) hash = unchecked(hash * 397 ^ child.GetHashCode());
            return hash;
        }
    }

    internal readonly struct Visual
    {
        internal readonly StaticObjectPrefab Source;
        internal readonly Matrix4x4 Transform;
        internal readonly int Index;
        internal Visual(StaticObjectPrefab source, Matrix4x4 transform, int index)
        { Source = source; Transform = transform; Index = index; }
    }

    internal static Node Plan(ObjectPrefab source)
    {
        CheckCycles(source, new HashSet<ObjectPrefab>());
        return Visit(source, true, selectedRoot: true);
    }

    internal static Node[] Plans(IReadOnlyList<ObjectSubObjectInfo> placements, int[]? modes = null)
    {
        // Modes: 1 direct moving root, 0 retained stationary root, -1 omitted gap.
        var plans = placements.Select((p, i) => modes == null || modes[i] == 1 ? Plan(p.m_Object) :
            new Node(p.m_Object, false, Array.Empty<ObjectSubObjectInfo>(), Array.Empty<Node>())).ToArray();
        var native = new HashSet<ObjectPrefab>();
        bool NeedsGroupIdentity(Node node) =>
            node.Source.GetComponent<SpawnableObject>()?.m_RandomizationGroup == null &&
            (node.Source.Has<UtilityObject>() || node.Source.Has<EffectSource>() ||
                node.Children.Any(child => !child.Merged || NeedsGroupIdentity(child)));
        void NativeClosure(ObjectPrefab source)
        {
            if (!native.Add(source)) return;
            foreach (var child in source.GetComponent<ObjectSubObjects>()?.m_SubObjects ?? Array.Empty<ObjectSubObjectInfo>())
                NativeClosure(child.m_Object);
        }
        void FindNative(Node node)
        {
            if (!node.Merged || native.Contains(node.Source) && NeedsGroupIdentity(node)) { NativeClosure(node.Source); return; }
            foreach (var child in node.Children) FindNative(child);
        }
        int previous;
        do
        {
            previous = native.Count;
            for (int i = 0; i < plans.Length; i++)
                if (modes == null || modes[i] >= 0) FindNative(plans[i]);
        } while (native.Count != previous);
        Node KeepSharedIdentity(Node node)
        {
            // The native implicit random group is the prefab identity. Do not fork
            // that identity when the same source also remains in a native branch.
            if (!node.Merged || native.Contains(node.Source) && NeedsGroupIdentity(node))
                return new Node(node.Source, false, Array.Empty<ObjectSubObjectInfo>(), Array.Empty<Node>());
            return new Node(node.Source, true, node.Records, node.Children.Select(KeepSharedIdentity).ToArray());
        }
        return plans.Select(KeepSharedIdentity).ToArray();
    }

    private static void CheckCycles(ObjectPrefab source, HashSet<ObjectPrefab> ancestry)
    {
        if (!ancestry.Add(source)) throw new InvalidOperationException("Nested attachment contains an ancestry cycle: " + source.name);
        try
        {
            foreach (var child in source.GetComponent<ObjectSubObjects>()?.m_SubObjects ?? Array.Empty<ObjectSubObjectInfo>())
                CheckCycles(child.m_Object, ancestry);
        }
        finally { ancestry.Remove(source); }
    }

    private static Node Visit(ObjectPrefab source, bool deterministic, bool selectedRoot = false)
    {
        var records = source.GetComponent<ObjectSubObjects>()?.m_SubObjects ?? Array.Empty<ObjectSubObjectInfo>();
        bool merge = deterministic && source is StaticObjectPrefab obj &&
            !source.Has<PlaceholderObject>() && (selectedRoot || !source.Has<ThemeObject>()) &&
            (!source.Has<EffectSource>() || source.Has<StreetLightObject>()) &&
            (obj.m_Meshes ?? Array.Empty<ObjectMeshInfo>()).All(m => m.m_RequireState == 0 && m.m_Mesh is RenderPrefab render &&
                TowerFactory.CanMergeRender(render, new HashSet<RenderPrefab>()));
        if (!merge) return new Node(source, false, Array.Empty<ObjectSubObjectInfo>(), Array.Empty<Node>());
        var children = new Node[records.Length];
        for (int i = 0; i < children.Length; i++)
        {
            // Native SubObjectSystem composes the object's local TR first, then
            // converts parent connection IDs to inverse-bind coordinates. A static
            // parent's closed frame does not acquire the mesh-slot transform twice.
            // Negative/stacked parents have separate native placement semantics.
            children[i] = Visit(records[i].m_Object, records[i].m_Probability == 100 &&
                records[i].m_ParentMesh >= 0 && records[i].m_ParentMesh < 1000);
        }
        return new Node(source, true, records.ToArray(), children);
    }

    internal static IReadOnlyList<Visual> Visuals(Node root)
    {
        var result = new List<Visual>();
        void Add(Node node, Matrix4x4 transform)
        {
            if (!node.Merged) return;
            result.Add(new Visual((StaticObjectPrefab)node.Source, transform, result.Count));
            for (int i = 0; i < node.Children.Length; i++)
            {
                var child = node.Records[i];
                Add(node.Children[i], transform * ChildTransform((StaticObjectPrefab)node.Source, child));
            }
        }
        Add(root, Matrix4x4.identity);
        return result;
    }

    // RenderingUtils.FindBoneIndex followed by RelativeObjectSystem's transform
    // evaluation, evaluated once for a stationary source skeleton. No history is
    // read or updated by this calculation; the result joins the deck's live bones.
    internal static Matrix4x4 ChildTransform(StaticObjectPrefab parent, ObjectSubObjectInfo child)
    {
        Vector3 position = child.m_Position; Quaternion rotation = child.m_Rotation;
        var meshes = parent.m_Meshes ?? Array.Empty<ObjectMeshInfo>();
        int connection = child.m_ParentMesh;
        if (connection > 0 && connection >= meshes.Length)
        {
            foreach (var info in meshes)
            {
                var animation = info.m_Mesh.GetComponent<ProceduralAnimationProperties>();
                var bones = animation?.m_Bones ?? Array.Empty<ProceduralAnimationProperties.BoneInfo>();
                for (int index = 0; index < bones.Length; index++)
                {
                    if (bones[index].m_ConnectionID != connection) continue;
                    Vector3 RestPosition(int i) => bones[i].parentId < 0 ? bones[i].position :
                        RestPosition(bones[i].parentId) + RestRotation(bones[i].parentId) * bones[i].position;
                    Quaternion RestRotation(int i) => bones[i].parentId < 0 ? bones[i].rotation :
                        RestRotation(bones[i].parentId) * bones[i].rotation;
                    var skin = StaticSkin(animation); // validates the parent chain before recursion above.
                    var objectPosition = (Vector3)info.m_Position + (Quaternion)info.m_Rotation * RestPosition(index);
                    var objectRotation = (Quaternion)info.m_Rotation * RestRotation(index);
                    var inverse = (Matrix4x4.TRS(objectPosition, objectRotation, Vector3.one) * bones[index].bindPose).inverse;
                    position = inverse.MultiplyPoint3x4(position);
                    rotation = Quaternion.LookRotation(inverse.MultiplyVector(rotation * Vector3.forward), inverse.MultiplyVector(rotation * Vector3.up));
                    position = skin[index].MultiplyPoint3x4(position);
                    rotation = Quaternion.LookRotation(skin[index].MultiplyVector(rotation * Vector3.forward), skin[index].MultiplyVector(rotation * Vector3.up));
                    position = (Vector3)info.m_Position + (Quaternion)info.m_Rotation * position;
                    rotation = (Quaternion)info.m_Rotation * rotation;
                    return Matrix4x4.TRS(position, rotation, Vector3.one);
                }
                connection++;
            }
        }
        return Matrix4x4.TRS(position, rotation, Vector3.one);
    }

    internal static Matrix4x4[] StaticSkin(ProceduralAnimationProperties? animation)
    {
        var bones = animation?.m_Bones ?? Array.Empty<ProceduralAnimationProperties.BoneInfo>();
        var world = new Matrix4x4[bones.Length]; var skin = new Matrix4x4[bones.Length];
        var state = new byte[bones.Length];
        void Evaluate(int index)
        {
            if (state[index] == 2) return;
            state[index] = 1; var bone = bones[index];
            if (bone.m_Type != BoneType.None) throw new InvalidOperationException("Independent animation cannot be evaluated as a static attachment.");
            var parent = Matrix4x4.identity;
            if (bone.parentId >= 0) { Evaluate(bone.parentId); parent = world[bone.parentId]; }
            world[index] = parent * Matrix4x4.TRS(bone.position, bone.rotation, bone.scale);
            skin[index] = world[index] * bone.bindPose;
            state[index] = 2;
        }
        for (int i = 0; i < bones.Length; i++) Evaluate(i);
        return skin;
    }

    internal static MovableStructureModel.Plan[] MeshPlans(Mesh mesh, Matrix4x4 transform,
        ProceduralAnimationProperties? animation = null)
    {
        var original = mesh.vertices;
        var target = new Vector3[original.Length];
        var frames = new KeyValuePair<int, Matrix4x4>[original.Length];
        var skin = StaticSkin(animation);
        using var raw = Mesh.AcquireReadOnlyMeshData(mesh);
        var hasIndices = mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.BlendIndices);
        int stream = hasIndices ? mesh.GetVertexAttributeStream(UnityEngine.Rendering.VertexAttribute.BlendIndices) : 0;
        int stride = hasIndices ? mesh.GetVertexBufferStride(stream) : 0;
        int offset = hasIndices ? mesh.GetVertexAttributeOffset(UnityEngine.Rendering.VertexAttribute.BlendIndices) : 0;
        for (int vertex = 0; vertex < original.Length; vertex++)
        {
            var matrix = transform;
            if (skin.Length != 0)
            {
                uint index = 0;
                if (hasIndices)
                {
                    var bytes = raw[0].GetVertexData<byte>(stream); int at = vertex * stride + offset;
                    // Native procedural shaders select the low eight bits of the packed bind index.
                    index = bytes[at];
                }
                // BatchDataHelpers supplies End - 1; the native vertex protocol
                // clamps its low-byte index to that last allocated source bone.
                matrix *= skin[Math.Min(index, (uint)skin.Length - 1)];
            }
            target[vertex] = matrix.MultiplyPoint3x4(original[vertex]);
            var direction = matrix; direction.m03 = direction.m13 = direction.m23 = 0f;
            frames[vertex] = new KeyValuePair<int, Matrix4x4>(vertex, direction);
        }
        var plans = new MovableStructureModel.Plan[mesh.subMeshCount];
        for (int slot = 0; slot < plans.Length; slot++)
        {
            var indices = mesh.GetIndices(slot);
            plans[slot] = new MovableStructureModel.Plan(original, target, new[] { indices },
                new[] { new bool[indices.Length / 3] }, frameTransforms: frames, nativeBoneFrames: skin.Length != 0);
        }
        return plans;
    }

    internal static ObjectPrefab Carrier(string owner, Node root, List<PrefabBase> created,
        Dictionary<Node, ObjectPrefab> cache)
    {
        ObjectPrefab Copy(Node node)
        {
            if (!node.Merged) return node.Source;
            if (cache.TryGetValue(node, out var existing)) return existing;
            var output = MovableRoadObjectCarrier.Create(owner, (StaticObjectPrefab)node.Source, created,
                preserveSelection: true);
            cache.Add(node, output);
            if (node.Records.Length != 0)
            {
                var children = output.GetComponent<ObjectSubObjects>();
                children.m_SubObjects = new ObjectSubObjectInfo[node.Records.Length];
                for (int i = 0; i < node.Records.Length; i++)
                {
                    var input = node.Records[i];
                    children.m_SubObjects[i] = new ObjectSubObjectInfo
                    {
                        m_Object = Copy(node.Children[i]), m_Position = input.m_Position,
                        m_Rotation = input.m_Rotation, m_ParentMesh = input.m_ParentMesh,
                        m_GroupIndex = input.m_GroupIndex, m_Probability = input.m_Probability
                    };
                }
            }
            return output;
        }
        return Copy(root);
    }
}
