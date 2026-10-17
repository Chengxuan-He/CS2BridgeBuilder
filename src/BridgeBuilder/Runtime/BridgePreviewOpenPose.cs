using System;
using Colossal.Mathematics;
using Unity.Mathematics;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Runtime;

// Preview-only settled native pose. Vertex skinning stays on the native GPU shader;
// only the small authored bone hierarchy is evaluated here. No prefab is mutated.
internal sealed class BridgePreviewOpenPose : IDisposable
{
    internal readonly Matrix4x4[] Skin, Delta;
    private readonly ComputeBuffer _buffer;

    internal BridgePreviewOpenPose(ProceduralAnimationProperties animation, Vector3 target)
    {
        var bones = animation.m_Bones;
        var count = bones.Length;
        Skin = new Matrix4x4[count]; Delta = new Matrix4x4[count];
        var rest = new Matrix4x4[count]; var opened = new Matrix4x4[count];
        var positions = new Vector3[count]; var rotations = new Quaternion[count];
        var state = new byte[count];
        for (var i = 0; i < count; i++) Evaluate(i);
        var matrices = new Matrix4x4[count * 2];
        Array.Copy(Skin, matrices, count);
        Array.Copy(Skin, 0, matrices, count, count);
        _buffer = new ComputeBuffer(count * 2, 64, ComputeBufferType.Raw);
        _buffer.SetData(matrices);

        void Evaluate(int i)
        {
            if (i < 0 || i >= count) throw new InvalidOperationException("Invalid preview bone reference.");
            if (state[i] == 2) return;
            if (state[i] == 1) throw new InvalidOperationException("Cyclic preview bone dependency.");
            state[i] = 1;
            var b = bones[i];
            var parent = Matrix4x4.identity; var restParent = Matrix4x4.identity;
            if (b.parentId >= 0)
            {
                Evaluate(b.parentId);
                parent = opened[b.parentId]; restParent = rest[b.parentId];
            }
            var p = b.position; var r = b.rotation;
            var localTarget = Quaternion.Inverse(r) * (parent.inverse.MultiplyPoint3x4(target) - p);
            switch (b.m_Type)
            {
                case BoneType.None: break;
                case BoneType.LookAtAimForward:
                    // ObjectInterpolateSystem: Left(target.yz), signed by speed,
                    // then RotationAngleSignedLeft(up.yz, direction).
                    var direction = MathUtils.Left(new float2(localTarget.y, localTarget.z));
                    if (b.m_Speed < 0f) direction = -direction;
                    r *= Quaternion.AngleAxis(MathUtils.RotationAngleSignedLeft(new float2(1f, 0f), math.normalize(direction)) * Mathf.Rad2Deg, Vector3.right);
                    break;
                case BoneType.LookAtMovementX:
                case BoneType.LookAtMovementY:
                case BoneType.LookAtMovementZ:
                    var axis = b.m_Type == BoneType.LookAtMovementX ? Vector3.right :
                        b.m_Type == BoneType.LookAtMovementY ? Vector3.up : Vector3.forward;
                    var movement = Vector3.Dot(localTarget, axis);
                    p += r * axis * (b.m_Speed < 0f ? -movement : movement);
                    break;
                case BoneType.RotationXFromMovementY:
                case BoneType.ScaledMovement:
                case BoneType.ScaledRotation:
                    var sourceIndex = Array.FindIndex(bones, candidate => candidate.m_ConnectionID == b.m_SourceID);
                    if (b.m_SourceID == 0 || sourceIndex < 0)
                        throw new InvalidOperationException("Missing movable preview source bone.");
                    Evaluate(sourceIndex);
                    var source = bones[sourceIndex];
                    if (b.m_Type == BoneType.ScaledMovement)
                        p += (positions[sourceIndex] - source.position) * b.m_Speed;
                    else if (b.m_Type == BoneType.ScaledRotation)
                        r *= Quaternion.SlerpUnclamped(Quaternion.identity,
                            Quaternion.Inverse(source.rotation) * rotations[sourceIndex], b.m_Speed);
                    else
                        // MeshSystem converts this speed from turns to radians.
                        r *= Quaternion.AngleAxis((positions[sourceIndex].y - source.position.y) *
                            b.m_Speed * 360f, Vector3.right);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported movable preview bone type: {b.m_Type}.");
            }
            positions[i] = p; rotations[i] = r;
            rest[i] = restParent * Matrix4x4.TRS(b.position, b.rotation, b.scale);
            opened[i] = parent * Matrix4x4.TRS(p, r, b.scale);
            Skin[i] = opened[i] * b.bindPose;
            Delta[i] = opened[i] * rest[i].inverse;
            state[i] = 2;
        }
    }

    internal void Bind(MaterialPropertyBlock block)
    {
        // Same buffer layout and property values as native RenderPrefabRenderer.Instance.
        block.SetBuffer("_BoneTransforms", _buffer);
        block.SetVector("colossal_BoneParameters", new Vector4(0f, Skin.Length, 0f, 0f));
        block.SetInt("_BonePreviousTransformsByteOffset", Skin.Length * 64);
    }

    public void Dispose() => _buffer.Dispose();
}
