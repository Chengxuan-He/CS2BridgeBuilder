// Generated from the game 1.6.2f1 native LiftBridge05 records.
// Source SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// Native serialized matrix elements are row-major; audit labels came from managed field order.
// Bind-pose field indices below are transposed once; numeric payloads are unchanged.
// Resource path IDs are extraction provenance only; none is used at runtime.
using System;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift05MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)2;
        pillar.m_AnchorOffset = 0f;
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-1f, 1f);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[] { (NetPieceRequirements)80 };
        net.m_RequireRoad = 0; net.m_RoadPassThrough = 0; net.m_TrackPassThrough = 0;
        net.m_NodeOffset = 84f;
        net.m_Attached = false; net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(10f, 58f, 10f);
        movement.m_MovingTime = 14f;
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(41.849998474121094f, 54f, 31.5f);
        standing.m_LegGap = new float2(0f, 123f);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != 3) throw new ArgumentException("Lift05 requires its main, pillar and pillar-base render graphs.");
        return new[]
        {
            new ObjectMeshInfo { m_Mesh = prepared[0], m_Position = new float3(0f, 0f, 0f), m_Rotation = new quaternion(0f, 0f, 0f, 1f), m_RequireState = 0 },
            new ObjectMeshInfo { m_Mesh = prepared[1], m_Position = new float3(0f, 0f, 0f), m_Rotation = new quaternion(0f, 0f, 0f, 1f), m_RequireState = 0 },
            new ObjectMeshInfo { m_Mesh = prepared[2], m_Position = new float3(0f, 0f, 0f), m_Rotation = new quaternion(0f, 0f, 0f, 1f), m_RequireState = 0 },
        };
    }

    // Full authored evidence. The generator replaces ordinary road attachments with
    // selected-road placements and widens structural attachments using measured plans.
    internal static ObjectSubObjectInfo[] AuthoredAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(-8f, 13f, -67.800788879394531f),
            m_Rotation = new quaternion(0f, 1f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(8f, 13f, -67.800773620605469f),
            m_Rotation = new quaternion(0f, 1f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(8f, 13f, 67.80078125f),
            m_Rotation = new quaternion(0f, 2.9802322387695312e-08f, 0f, 1f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(-8f, 13f, 67.80078125f),
            m_Rotation = new quaternion(0f, -2.9802322387695312e-08f, 0f, 1f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(-9.8000001907348633f, 75.699996948242188f, 69.5f),
            m_Rotation = new quaternion(0f, 2.9802322387695312e-08f, 0f, 1f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(9.8000001907348633f, 75.699996948242188f, 69.5f),
            m_Rotation = new quaternion(0f, 2.9802322387695312e-08f, 0f, 1f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(9.8000001907348633f, 75.699996948242188f, 60.5f),
            m_Rotation = new quaternion(0f, 1f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(-9.8000001907348633f, 75.699996948242188f, 60.5f),
            m_Rotation = new quaternion(0f, 1f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(9.8000001907348633f, 75.699996948242188f, -69.5f),
            m_Rotation = new quaternion(0f, 1f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(-9.8000001907348633f, 75.699996948242188f, -69.5f),
            m_Rotation = new quaternion(0f, 1f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(-9.8000001907348633f, 75.699996948242188f, -60.5f),
            m_Rotation = new quaternion(0f, 5.9604644775390625e-08f, 0f, 1f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(9.8000001907348633f, 75.699996948242188f, -60.5f),
            m_Rotation = new quaternion(0f, -2.9802322387695312e-08f, 0f, 1f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-8.5f, 78.75f, -67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-8.5f, 78.75f, -63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-3f, 78.75f, -63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-3f, 78.75f, -67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(3f, 78.75f, -67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(3f, 78.75f, -63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(8.5f, 78.75f, -63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(8.5f, 78.75f, -67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-8.5f, 78.75f, 67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-8.5f, 78.75f, 63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-3f, 78.75f, 63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(-3f, 78.75f, 67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(3f, 78.75f, 67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(3f, 78.75f, 63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(8.5f, 78.75f, 63f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(8.5f, 78.75f, 67f),
            m_Rotation = new quaternion(0f, 1f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
    };

    internal static void ApplySkeleton(RenderPrefab target, int lod)
    {
        var animation = target.AddComponent<ProceduralAnimationProperties>();
        animation.m_Animations = Array.Empty<ProceduralAnimationProperties.AnimationInfo>();
        animation.m_Bones = lod switch
        {
            0 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge05_Root",
                    position = new Vector3(0f, 0f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = 0f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = -1, m_Type = (BoneType)0,
                    m_Speed = 0f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "LiftBridge05_Weights",
                    position = new Vector3(0f, 65.800003051757812f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -65.800003051757812f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)32,
                    m_Speed = -1f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 100
                },
                new()
                {
                    name = "LiftBridge05_Tracks",
                    position = new Vector3(0f, 10f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -10f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)28,
                    m_Speed = 6f, m_Acceleration = 1.2000000476837158f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge05_Root",
                    position = new Vector3(0f, 0f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = 0f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = -1, m_Type = (BoneType)0,
                    m_Speed = 0f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "LiftBridge05_Weights",
                    position = new Vector3(0f, 65.800003051757812f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -65.800003051757812f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)32,
                    m_Speed = -1f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 100
                },
                new()
                {
                    name = "LiftBridge05_Tracks",
                    position = new Vector3(0f, 10f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -10f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)28,
                    m_Speed = 6f, m_Acceleration = 1.2000000476837158f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge05_Root",
                    position = new Vector3(0f, 0f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = 0f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = -1, m_Type = (BoneType)0,
                    m_Speed = 0f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "LiftBridge05_Weights",
                    position = new Vector3(0f, 65.800003051757812f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -65.800003051757812f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)32,
                    m_Speed = -1f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 100
                },
                new()
                {
                    name = "LiftBridge05_Tracks",
                    position = new Vector3(0f, 10f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -10f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)28,
                    m_Speed = 6f, m_Acceleration = 1.2000000476837158f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
    }
}
