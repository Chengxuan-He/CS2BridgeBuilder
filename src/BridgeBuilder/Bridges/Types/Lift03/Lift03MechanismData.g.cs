// Generated from the game 1.6.2f1 native LiftBridge03 records.
// Source SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// Resource path IDs are extraction provenance only; none is used at runtime.
using System;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class Lift03MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)2;
        pillar.m_AnchorOffset = 91.919197082519531f;
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-1f, 1f);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[] { (NetPieceRequirements)80 };
        net.m_RequireRoad = 0; net.m_RoadPassThrough = 0; net.m_TrackPassThrough = 0;
        net.m_NodeOffset = 127.96520233154297f;
        net.m_Attached = false; net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(10f, 73f, 10f);
        movement.m_MovingTime = 18f;
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(93.239997863769531f, 67f, 50f);
        standing.m_LegGap = new float2(0f, 135f);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != 3) throw new ArgumentException("Lift03 requires its main, pillar and pillar-base render graphs.");
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
            m_Object = resolve("WarningLight01"),
            m_Position = new float3(-24.5f, 108.59999847412109f, 96.199996948242188f),
            m_Rotation = new quaternion(0f, 0f, 0f, 0.99999994039535522f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WarningLight01"),
            m_Position = new float3(24.5f, 108.59999847412109f, 96.199996948242188f),
            m_Rotation = new quaternion(0f, 0f, 0f, 0.99999994039535522f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WarningLight01"),
            m_Position = new float3(24.5f, 108.59999847412109f, -96.199996948242188f),
            m_Rotation = new quaternion(0f, 0.99999994039535522f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WarningLight01"),
            m_Position = new float3(-24.5f, 108.59999847412109f, -96.199996948242188f),
            m_Rotation = new quaternion(0f, 0.99999994039535522f, 0f, -2.9802322387695312e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.802877426147461f, 32.81805419921875f, 102.22055053710938f),
            m_Rotation = new quaternion(-1.2011854551019496e-06f, -1.2011854551019496e-06f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.767551422119141f, 50.391357421875f, 101.5233154296875f),
            m_Rotation = new quaternion(7.8331322583835572e-05f, 7.8331322583835572e-05f, -0.70710676908493042f, -0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.735115051269531f, 67.97100830078125f, 100.47960662841797f),
            m_Rotation = new quaternion(0f, 0f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.765995025634766f, 85.5926513671875f, 99.084197998046875f),
            m_Rotation = new quaternion(0f, 0f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.720947265625f, 32.76519775390625f, 102.22196960449219f),
            m_Rotation = new quaternion(2.4234441298176534e-06f, 2.4234441298176534e-06f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.708705902099609f, 50.379638671875f, 101.52388000488281f),
            m_Rotation = new quaternion(0f, 0f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.791513442993164f, 67.9842529296875f, 100.47867584228516f),
            m_Rotation = new quaternion(0f, 0f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.703067779541016f, 85.6829833984375f, 99.075950622558594f),
            m_Rotation = new quaternion(0f, 0f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.745594024658203f, 103.0859375f, 97.216339111328125f),
            m_Rotation = new quaternion(0f, 0f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.787242889404297f, 32.75787353515625f, -102.22218322753906f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, -3.0908619663705394e-08f, -3.0908619663705394e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.823348999023438f, 32.67333984375f, -102.22450256347656f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, -3.0908619663705394e-08f, -3.0908619663705394e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.808320999145508f, 50.22515869140625f, -101.53147125244141f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, -3.0908619663705394e-08f, -3.0908619663705394e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.865568161010742f, 50.22198486328125f, -101.53160095214844f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, -3.0908619663705394e-08f, -3.0908619663705394e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.781242370605469f, 68.0782470703125f, -100.47219848632812f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, -1.1520231879558196e-07f, -1.1520231879558196e-07f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.788845062255859f, 67.98504638671875f, -100.47865295410156f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, 5.0629601901164278e-05f, 5.0629601901164278e-05f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.791839599609375f, 85.704833984375f, -99.073921203613281f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, 5.0629601901164278e-05f, 5.0629601901164278e-05f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.822734832763672f, 85.6318359375f, -99.080635070800781f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, -3.0908619663705394e-08f, -3.0908619663705394e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.747310638427734f, 103.04010009765625f, 97.221771240234375f),
            m_Rotation = new quaternion(8.4293702684590244e-08f, 8.4293702684590244e-08f, 0.70710676908493042f, 0.70710676908493042f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(19.826950073242188f, 103.02398681640625f, -97.223686218261719f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, -3.0908619663705394e-08f, -3.0908619663705394e-08f),
            m_ParentMesh = 0, m_GroupIndex = 0, m_Probability = 100
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightTwowayFarRange01"),
            m_Position = new float3(-19.815330505371094f, 103.039794921875f, -97.221817016601562f),
            m_Rotation = new quaternion(0.70710676908493042f, 0.70710676908493042f, 2.2197245641564223e-07f, 2.2197245641564223e-07f),
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
                    name = "LiftBridge03_Root",
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
                    name = "LiftBridge03_WheelR",
                    position = new Vector3(-0f, 104.18754577636719f, -87.248336791992188f),
                    rotation = new Quaternion(6.1232342629258393e-17f, -1f, -6.1232342629258393e-17f, 6.1232336011813492e-17f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = -1f, m10 = 1.2246468525851679e-16f, m20 = 1.2246468525851679e-16f, m30 = 2.3444135562457162e-14f, m01 = 1.2246468525851679e-16f, m11 = 1f, m21 = 1.2246468525851679e-16f, m31 = -104.18754577636719f, m02 = -1.2246468525851679e-16f, m12 = 1.2246468525851679e-16f, m22 = -1f, m32 = -87.248336791992188f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)31,
                    m_Speed = 0.039999999105930328f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 100
                },
                new()
                {
                    name = "LiftBridge03_WheelL",
                    position = new Vector3(-0f, 104.18754577636719f, 87.248359680175781f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = -0f, m30 = -0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -104.18754577636719f, m02 = -0f, m12 = 0f, m22 = 1f, m32 = -87.248359680175781f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)31,
                    m_Speed = 0.039999999105930328f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 100
                },
                new()
                {
                    name = "LiftBridge03_Road",
                    position = new Vector3(-0f, 10f, 0f),
                    rotation = new Quaternion(0f, -0f, -0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = -0f, m30 = -0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -10f, m02 = -0f, m12 = 0f, m22 = 1f, m32 = 0f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)28,
                    m_Speed = 5f, m_Acceleration = 1f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge03_Root",
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
                    name = "LiftBridge03_WheelR",
                    position = new Vector3(0f, 104.18754577636719f, -87.248336791992188f),
                    rotation = new Quaternion(6.1232342629258393e-17f, -1f, -6.1232342629258393e-17f, 6.1232336011813492e-17f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = -1f, m10 = 1.2246468525851679e-16f, m20 = 1.2246468525851679e-16f, m30 = 2.3444135562457162e-14f, m01 = 1.2246468525851679e-16f, m11 = 1f, m21 = 1.2246468525851679e-16f, m31 = -104.18754577636719f, m02 = -1.2246468525851679e-16f, m12 = 1.2246468525851679e-16f, m22 = -1f, m32 = -87.248336791992188f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)31,
                    m_Speed = 0.039999999105930328f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "LiftBridge03_WheelL",
                    position = new Vector3(0f, 104.18754577636719f, 87.248359680175781f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -104.18754577636719f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = -87.248359680175781f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)31,
                    m_Speed = 0.039999999105930328f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "LiftBridge03_Road",
                    position = new Vector3(0f, 10f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -10f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = 0f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)28,
                    m_Speed = 5f, m_Acceleration = 1f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge03_Root",
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
                    name = "LiftBridge03_WheelR",
                    position = new Vector3(0f, 104.18754577636719f, -87.248336791992188f),
                    rotation = new Quaternion(6.1232342629258393e-17f, -1f, -6.1232342629258393e-17f, 6.1232336011813492e-17f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = -1f, m10 = 1.2246468525851679e-16f, m20 = 1.2246468525851679e-16f, m30 = 2.3444135562457162e-14f, m01 = 1.2246468525851679e-16f, m11 = 1f, m21 = 1.2246468525851679e-16f, m31 = -104.18754577636719f, m02 = -1.2246468525851679e-16f, m12 = 1.2246468525851679e-16f, m22 = -1f, m32 = -87.248336791992188f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)31,
                    m_Speed = 0.039999999105930328f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "LiftBridge03_WheelL",
                    position = new Vector3(0f, 104.18754577636719f, 87.248359680175781f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -104.18754577636719f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = -87.248359680175781f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)31,
                    m_Speed = 0.039999999105930328f, m_Acceleration = 0f,
                    m_ConnectionID = 0, m_SourceID = 0
                },
                new()
                {
                    name = "LiftBridge03_Road",
                    position = new Vector3(0f, 10f, 0f),
                    rotation = new Quaternion(0f, 0f, 0f, 1f),
                    scale = new Vector3(1f, 1f, 1f),
                    bindPose = new Matrix4x4 { m00 = 1f, m10 = 0f, m20 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = -10f, m02 = 0f, m12 = 0f, m22 = 1f, m32 = 0f, m03 = 0f, m13 = 0f, m23 = 0f, m33 = 1f },
                    parentId = 0, m_Type = (BoneType)28,
                    m_Speed = 5f, m_Acceleration = 1f,
                    m_ConnectionID = 100, m_SourceID = 0
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))
        };
    }
}
