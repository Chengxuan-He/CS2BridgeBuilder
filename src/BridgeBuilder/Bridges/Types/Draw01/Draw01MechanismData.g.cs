// Generated from the game 1.6.2f1 native DrawBridge01 records.
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

internal static class Draw01MechanismData
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string name,
        RenderPrefab[] preparedMeshes, ObjectSubObjectInfo[] preparedAttachments, ContentPrefab content)
        => factory.CreateMovableObject(name, false, Meshes(preparedMeshes), preparedAttachments,
            target => ApplyComponents(target, content));

    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)2;
        pillar.m_AnchorOffset = 70.099998474121094f;
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-1f, 1f);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[] { (NetPieceRequirements)80 };
        net.m_RequireRoad = 0; net.m_RoadPassThrough = 0; net.m_TrackPassThrough = 0;
        net.m_NodeOffset = 90f;
        net.m_Attached = false; net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(6.619999885559082f, 160f, 6.619999885559082f);
        movement.m_MovingTime = 15f;
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(15.199999809265137f, 80f, 15.300000190734863f);
        standing.m_LegGap = new float2(0f, 154.19999694824219f);
        standing.m_CircularLeg = false;
    }

    private static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != 3) throw new ArgumentException("Draw01 requires its main, pillar and pillar-base render graphs.");
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
            m_Position = new float3(4f, 10f, -60f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(4f, 10f, -20f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 100, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(4f, 10f, 60f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(4f, 10f, 20f),
            m_Rotation = new quaternion(0f, -0.7071068286895752f, 0f, 0.7071068286895752f),
            m_ParentMesh = 101, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(6.9824075698852539f, 6.063232421875f, 83.999992370605469f),
            m_Rotation = new quaternion(0f, 0.99999982118606567f, 0f, 8.9406967163085938e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(7.8587589263916016f, 12.29296875f, 88.239860534667969f),
            m_Rotation = new quaternion(0f, -0.70710653066635132f, 0f, 0.7071068286895752f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(7.8587627410888672f, 12.300048828125f, -88.256790161132812f),
            m_Rotation = new quaternion(0f, -0.70710694789886475f, 0f, 0.70710641145706177f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(7.9282197952270508f, 15.61859130859375f, -86.499992370605469f),
            m_Rotation = new quaternion(0f, 0f, 0f, 0.99999994039535522f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(6.9707098007202148f, 6.03759765625f, -84.000007629394531f),
            m_Rotation = new quaternion(0f, 0f, 0f, 0.99999994039535522f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(7.9300556182861328f, 15.64581298828125f, 86.500015258789062f),
            m_Rotation = new quaternion(0f, 0.99999994039535522f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(10.01465892791748f, 16.22900390625f, 89.382720947265625f),
            m_Rotation = new quaternion(0f, 0.99999994039535522f, 0f, 0f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof05"),
            m_Position = new float3(7.0683526992797852f, 16.22900390625f, -89.515426635742188f),
            m_Rotation = new quaternion(0f, 0.99999994039535522f, 0f, 0f),
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
                    name = "DrawBridge01_Root",
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
                    name = "DrawBridge01_Right",
                    position = new Vector3(-0f, 6.619999885559082f, -81.300003051757812f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = -0f, m03 = -0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -6.619999885559082f, m20 = -0f, m21 = 0f, m22 = 1f, m23 = 81.300003051757812f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = -0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge01_Left",
                    position = new Vector3(-0f, 6.619999885559082f, 81.300003051757812f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = -0f, m03 = -0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -6.619999885559082f, m20 = -0f, m21 = 0f, m22 = 1f, m23 = -81.300003051757812f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge01_Root",
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
                    name = "DrawBridge01_Right",
                    position = new Vector3(0f, 6.619999885559082f, -81.300003051757812f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -6.619999885559082f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 81.300003051757812f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = -0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge01_Left",
                    position = new Vector3(0f, 6.619999885559082f, 81.300003051757812f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -6.619999885559082f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = -81.300003051757812f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge01_Root",
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
                    name = "DrawBridge01_Right",
                    position = new Vector3(0f, 6.619999885559082f, -81.300003051757812f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -6.619999885559082f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = 81.300003051757812f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = -0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
                new()
                {
                    name = "DrawBridge01_Left",
                    position = new Vector3(0f, 6.619999885559082f, 81.300003051757812f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m01 = 0f, m02 = 0f, m03 = 0f, m10 = 0f, m11 = 1f, m12 = 0f, m13 = -6.619999885559082f, m20 = 0f, m21 = 0f, m22 = 1f, m23 = -81.300003051757812f, m30 = 0f, m31 = 0f, m32 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)33,
                    m_Speed = 0.014999999664723873f, m_Acceleration = 0.0030000000260770321f,
                    m_ConnectionID = 101, m_SourceID = 0
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
    }
}
