// Generated from the game 1.6.2f1 native DrawBridge02 records.
// Source SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// Resource path IDs are extraction provenance only; none is used at runtime.
using System;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Draw02MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)2;
        pillar.m_AnchorOffset = 65.099998474121094f;
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-1f, 1f);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[] { (NetPieceRequirements)80 };
        net.m_RequireRoad = 0; net.m_RoadPassThrough = 0; net.m_TrackPassThrough = 0;
        net.m_NodeOffset = 90f;
        net.m_Attached = false; net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(5.4140000343322754f, 140f, 5.4140000343322754f);
        movement.m_MovingTime = 16f;
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(32.939998626708984f, 75f, 26.100000381469727f);
        standing.m_LegGap = new float2(0f, 133f);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != 3) throw new ArgumentException("Draw02 requires its main, pillar and pillar-base render graphs.");
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
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(7f, 10f, -60f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(7f, 10f, -20f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(7f, 10f, 20f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(7f, 10f, 60f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentWall05"),
            m_Position = new float3(-13.451326370239258f, 19.4765625f, 88.699234008789062f),
            m_Rotation = new quaternion(0f, 0f, 0f, 0.99999994039535522f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentWall05"),
            m_Position = new float3(-13.37742805480957f, 14.5206298828125f, 88.699195861816406f),
            m_Rotation = new quaternion(0f, 0f, 0f, 0.99999994039535522f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(-12.750017166137695f, 12.42041015625f, 84.966682434082031f),
            m_Rotation = new quaternion(0f, 0.70715737342834473f, 0f, 0.70705598592758179f),
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
                    name = "DrawBridge02_Root",
                    position = new Vector3(-0f, 0f, 0f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = -0f, m30 = -0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = 0f, m02 = -0f, m12 = 0f, m22 = 1f, m32 = 0f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = -1, m_Type = (BoneType)0,
                    m_Speed = 0f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge02_Left",
                    position = new Vector3(-0f, 5.4140000343322754f, 73.980003356933594f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = -0f, m30 = -0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -5.4140000343322754f, m02 = -0f, m12 = 0f, m22 = 1f, m32 = -73.980003356933594f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge02_Right",
                    position = new Vector3(-0f, 5.4140000343322754f, -73.980003356933594f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = -0f, m30 = -0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -5.4140000343322754f, m02 = -0f, m12 = 0f, m22 = 1f, m32 = 73.980003356933594f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = -0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge02_Root",
                    position = new Vector3(0f, 0f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = 0f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = 0f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = -1, m_Type = (BoneType)0,
                    m_Speed = 0f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge02_Left",
                    position = new Vector3(0f, 5.4140000343322754f, 73.980003356933594f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -5.4140000343322754f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = -73.980003356933594f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge02_Right",
                    position = new Vector3(0f, 5.4140000343322754f, -73.980003356933594f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -5.4140000343322754f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = 73.980003356933594f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = -0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge02_Root",
                    position = new Vector3(0f, 0f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = 0f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = 0f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = -1, m_Type = (BoneType)0,
                    m_Speed = 0f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge02_Left",
                    position = new Vector3(0f, 5.4140000343322754f, 73.980003356933594f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -5.4140000343322754f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = -73.980003356933594f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge02_Right",
                    position = new Vector3(0f, 5.4140000343322754f, -73.980003356933594f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -5.4140000343322754f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = 73.980003356933594f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = -0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
    }
}
