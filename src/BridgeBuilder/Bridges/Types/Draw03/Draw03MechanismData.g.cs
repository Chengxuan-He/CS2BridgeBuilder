// Generated from the game 1.6.2f1 native DrawBridge03 records.
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

internal static class Draw03MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)2;
        pillar.m_AnchorOffset = 64.900001525878906f;
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-1f, 1f);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[] { (NetPieceRequirements)80 };
        net.m_RequireRoad = 0; net.m_RoadPassThrough = 0; net.m_TrackPassThrough = 0;
        net.m_NodeOffset = 96f;
        net.m_Attached = false; net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(4.0459718704223633f, 140f, 4.0459718704223633f);
        movement.m_MovingTime = 16f;
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(57.400001525878906f, 75f, 19.5f);
        standing.m_LegGap = new float2(0f, 141f);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != 3) throw new ArgumentException("Draw03 requires its main, pillar and pillar-base render graphs.");
        return new[]
        {
            new ObjectMeshInfo { m_Mesh = prepared[0], m_Position = new float3(0f, 0f, 0f), m_Rotation = new quaternion(0f, 0f, 0f, 1f), m_RequireState = 0 },
            new ObjectMeshInfo { m_Mesh = prepared[1], m_Position = new float3(0f, 0f, 0f), m_Rotation = new quaternion(0f, 0f, 0f, 1f), m_RequireState = 0 },
            new ObjectMeshInfo { m_Mesh = prepared[2], m_Position = new float3(0f, 0f, 0f), m_Rotation = new quaternion(0f, 0f, 0f, 1f), m_RequireState = 0 },
        };
    }

    // Native road lamps supply longitudinal stations; the selected road supplies lamp types.
    internal static ObjectSubObjectInfo[] RoadAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(10f, 10f, 20f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(-10f, 10f, 60f),
            m_Rotation = new quaternion(0f, 0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(10f, 10f, -20f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(-10f, 10f, 20f),
            m_Rotation = new quaternion(0f, 0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(10f, 10f, 60f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(-10f, 10f, -20f),
            m_Rotation = new quaternion(0f, 0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(-10f, 10f, -60f),
            m_Rotation = new quaternion(0f, 0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(10f, 10f, -60f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
    };

    // Two fixed flagpoles and ten wall lights; all retain native parent connection 0.
    internal static ObjectSubObjectInfo[] StructuralAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("FlagPole01"),
            m_Position = new float3(-21.5f, 10.2001953125f, -77.199996948242188f),
            m_Rotation = new quaternion(0f, 0.99999988079071045f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("FlagPole01"),
            m_Position = new float3(21.5f, 10.2001953125f, 77.199996948242188f),
            m_Rotation = new quaternion(0f, 0f, 0f, 1f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(18.063997268676758f, 12.561767578125f, -79.777366638183594f),
            m_Rotation = new quaternion(0f, 0.99999994039535522f, 0f, -5.9604644775390625e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(17.177215576171875f, 5.57977294921875f, -82.455375671386719f),
            m_Rotation = new quaternion(0f, 0.70710700750350952f, 0f, 0.70710641145706177f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(-17.177274703979492f, 5.57647705078125f, -82.44073486328125f),
            m_Rotation = new quaternion(0f, -0.70710694789886475f, 0f, 0.70710641145706177f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(-15.001517295837402f, 5.58465576171875f, -71.931671142578125f),
            m_Rotation = new quaternion(0f, 2.9802322387695312e-07f, 0f, 0.99999982118606567f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(15.003774642944336f, 5.57159423828125f, -71.931625366210938f),
            m_Rotation = new quaternion(0f, 1.1920928955078125e-07f, 0f, 0.99999988079071045f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(14.997523307800293f, 5.5770263671875f, 71.931625366210938f),
            m_Rotation = new quaternion(0f, 0.99999988079071045f, 0f, 0.00011026859283447266f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(17.177227020263672f, 5.58050537109375f, 82.444732666015625f),
            m_Rotation = new quaternion(0f, 0.70698469877243042f, 0f, 0.70722860097885132f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(-18.077098846435547f, 12.5579833984375f, 79.777328491210938f),
            m_Rotation = new quaternion(0f, 0f, 0f, 0.99999994039535522f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(-17.177251815795898f, 5.57293701171875f, 82.46826171875f),
            m_Rotation = new quaternion(0f, -0.70710647106170654f, 0f, 0.70710694789886475f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange02"),
            m_Position = new float3(-14.983791351318359f, 5.588134765625f, 71.931640625f),
            m_Rotation = new quaternion(0f, 0.99999982118606567f, 0f, 8.9406967163085938e-08f),
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
                    name = "DrawBridge03_Root",
                    position = new Vector3(-0f, 0f, 0f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = -0f, m03 = -0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = 0f, m20 = -0f, m21 = 0f, m22 = 1f, m23 = 0f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = -1, m_Type = (BoneType)0,
                    m_Speed = 0f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge03_LeftSide",
                    position = new Vector3(-0f, 4.0459723472595215f, 75.3839111328125f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = -0f, m03 = -0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -4.0459723472595215f, m20 = -0f, m21 = 0f, m22 = 1f, m23 = -75.3839111328125f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge03_RightSide",
                    position = new Vector3(-0f, 4.0459718704223633f, -75.383903503417969f),
                    rotation = new Quaternion(6.1232342629258393e-17f, -1f, -6.1232342629258393e-17f, -7.5497901264043321e-08f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = -1f, m01 = 1.2246468525851679e-16f, m02 = -1.5099580252808664e-07f, m03 = -1.1382652701286133e-05f, m10 = 1.2246469849340659e-16f, m11 = 1f, m12 = 1.2246468525851679e-16f, m13 = -4.0459718704223633f, m20 = 1.5099580252808664e-07f, m21 = 1.2246469849340659e-16f, m22 = -1f, m23 = -75.383903503417969f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge03_Root",
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
                    name = "DrawBridge03_LeftSide",
                    position = new Vector3(0f, 4.0459723472595215f, 75.3839111328125f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -4.0459723472595215f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = -75.3839111328125f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge03_RightSide",
                    position = new Vector3(0f, 4.0459718704223633f, -75.383903503417969f),
                    rotation = new Quaternion(6.1232342629258393e-17f, -1f, -6.1232342629258393e-17f, -7.5497901264043321e-08f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = -1f, m01 = 1.2246468525851679e-16f, m02 = -1.5099580252808664e-07f, m03 = -1.1382652701286133e-05f, m10 = 1.2246469849340659e-16f, m11 = 1f, m12 = 1.2246468525851679e-16f, m13 = -4.0459718704223633f, m20 = 1.5099580252808664e-07f, m21 = 1.2246469849340659e-16f, m22 = -1f, m23 = -75.383903503417969f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge03_Root",
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
                    name = "DrawBridge03_LeftSide",
                    position = new Vector3(0f, 4.0459723472595215f, 75.3839111328125f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -4.0459723472595215f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = -75.3839111328125f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge03_RightSide",
                    position = new Vector3(0f, 4.0459718704223633f, -75.383903503417969f),
                    rotation = new Quaternion(6.1232342629258393e-17f, -1f, -6.1232342629258393e-17f, -7.5497901264043321e-08f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = -1f, m01 = 1.2246468525851679e-16f, m02 = -1.5099580252808664e-07f, m03 = -1.1382652701286133e-05f, m10 = 1.2246469849340659e-16f, m11 = 1f, m12 = 1.2246468525851679e-16f, m13 = -4.0459718704223633f, m20 = 1.5099580252808664e-07f, m21 = 1.2246469849340659e-16f, m22 = -1f, m23 = -75.383903503417969f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
    }
}
