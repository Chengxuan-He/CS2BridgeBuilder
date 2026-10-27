// Generated from the game 1.6.2f1 native PedestrianDrawBridge01 records.
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
internal static class PedestrianDraw01MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[0];
        pillar.m_AnchorOffset = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[0];
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[1], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[2]);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[]
        {
            (NetPieceRequirements)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[1]
        };
        net.m_RequireRoad = (Game.Net.RoadTypes)(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[2]);
        net.m_RoadPassThrough = (Game.Net.RoadTypes)(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[3]);
        net.m_TrackPassThrough = (Game.Net.TrackTypes)(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[4]);
        net.m_NodeOffset = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[3];
        net.m_Attached = false;
        net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[4], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[5], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[6]);
        movement.m_MovingTime = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[7];
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[8], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[9], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[10]);
        standing.m_LegGap = new float2(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[11], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[12]);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[5])
            throw new ArgumentException("PedestrianDraw01 requires its main, pillar and pillar-base render graphs.");
        return new[]
        {
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[6]],
                m_Position = new float3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[13], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[14], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[15]),
                m_Rotation = new quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[16], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[17], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[18], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[19]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[7])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[8]],
                m_Position = new float3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[20], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[21], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[22]),
                m_Rotation = new quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[23], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[24], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[25], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[26]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[9])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[10]],
                m_Position = new float3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[27], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[28], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[29]),
                m_Rotation = new quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[30], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[31], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[32], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[33]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[11])
            },
        };
    }

    // Full authored evidence. The generator replaces ordinary road attachments with
    // selected-road placements and widens structural attachments using measured plans.
    internal static ObjectSubObjectInfo[] AuthoredAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof01"),
            m_Position = new float3(-Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[34], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[35], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[36]),
            m_Rotation = new quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[37], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[38], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[39], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[40]),
            m_ParentMesh = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[12],
            m_GroupIndex = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[13],
            m_Probability = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[14]
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
                    name = "PedestrianDrawBridge01_Root",
                    position = new Vector3(-Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[41], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[42], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[43]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[44], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[45], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[46], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[47]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[48], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[49], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[50]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[51],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[52],
                        m02 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[53],
                        m03 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[54],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[55],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[56],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[57],
                        m13 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[58],
                        m20 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[59],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[60],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[61],
                        m23 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[62],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[63],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[64],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[65],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[66]
                    },
                    parentId = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[15],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[16],
                    m_Speed = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[67],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[68],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[17],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[18]
                },
                new()
                {
                    name = "PedestrianDrawBridge01_Left",
                    position = new Vector3(-Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[69], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[70], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[71]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[72], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[73], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[74], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[75]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[76], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[77], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[78]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[79],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[80],
                        m02 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[81],
                        m03 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[82],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[83],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[84],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[85],
                        m13 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[86],
                        m20 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[87],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[88],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[89],
                        m23 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[90],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[91],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[92],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[93],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[94]
                    },
                    parentId = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[19],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[20],
                    m_Speed = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[95],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[96],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[21],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[22]
                },
                new()
                {
                    name = "PedestrianDrawBridge01_Right",
                    position = new Vector3(-Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[97], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[98], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[99]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[100], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[101], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[102], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[103]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[104], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[105], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[106]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[107],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[108],
                        m02 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[109],
                        m03 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[110],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[111],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[112],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[113],
                        m13 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[114],
                        m20 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[115],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[116],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[117],
                        m23 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[118],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[119],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[120],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[121],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[122]
                    },
                    parentId = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[23],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[24],
                    m_Speed = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[123],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[124],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[25],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[26]
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "PedestrianDrawBridge01_Root",
                    position = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[125], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[126], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[127]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[128], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[129], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[130], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[131]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[132], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[133], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[134]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[135],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[136],
                        m02 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[137],
                        m03 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[138],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[139],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[140],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[141],
                        m13 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[142],
                        m20 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[143],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[144],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[145],
                        m23 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[146],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[147],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[148],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[149],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[150]
                    },
                    parentId = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[27],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[28],
                    m_Speed = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[151],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[152],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[29],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[30]
                },
                new()
                {
                    name = "PedestrianDrawBridge01_Left",
                    position = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[153], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[154], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[155]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[156], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[157], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[158], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[159]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[160], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[161], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[162]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[163],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[164],
                        m02 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[165],
                        m03 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[166],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[167],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[168],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[169],
                        m13 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[170],
                        m20 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[171],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[172],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[173],
                        m23 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[174],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[175],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[176],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[177],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[178]
                    },
                    parentId = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[31],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[32],
                    m_Speed = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[179],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[180],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[33],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[34]
                },
                new()
                {
                    name = "PedestrianDrawBridge01_Right",
                    position = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[181], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[182], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[183]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[184], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[185], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[186], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[187]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[188], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[189], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[190]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[191],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[192],
                        m02 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[193],
                        m03 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[194],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[195],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[196],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[197],
                        m13 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[198],
                        m20 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[199],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[200],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[201],
                        m23 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[202],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[203],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[204],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[205],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[206]
                    },
                    parentId = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[35],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[36],
                    m_Speed = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[207],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[208],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[37],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[38]
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "PedestrianDrawBridge01_Root",
                    position = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[209], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[210], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[211]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[212], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[213], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[214], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[215]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[216], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[217], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[218]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[219],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[220],
                        m02 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[221],
                        m03 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[222],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[223],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[224],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[225],
                        m13 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[226],
                        m20 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[227],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[228],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[229],
                        m23 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[230],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[231],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[232],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[233],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[234]
                    },
                    parentId = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[39],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[40],
                    m_Speed = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[235],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[236],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[41],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[42]
                },
                new()
                {
                    name = "PedestrianDrawBridge01_Left",
                    position = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[237], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[238], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[239]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[240], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[241], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[242], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[243]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[244], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[245], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[246]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[247],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[248],
                        m02 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[249],
                        m03 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[250],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[251],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[252],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[253],
                        m13 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[254],
                        m20 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[255],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[256],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[257],
                        m23 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[258],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[259],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[260],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[261],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[262]
                    },
                    parentId = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[43],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[44],
                    m_Speed = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[263],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[264],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[45],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[46]
                },
                new()
                {
                    name = "PedestrianDrawBridge01_Right",
                    position = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[265], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[266], -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[267]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[268], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[269], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[270], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[271]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[272], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[273], Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[274]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[275],
                        m01 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[276],
                        m02 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[277],
                        m03 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[278],
                        m10 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[279],
                        m11 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[280],
                        m12 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[281],
                        m13 = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[282],
                        m20 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[283],
                        m21 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[284],
                        m22 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[285],
                        m23 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[286],
                        m30 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[287],
                        m31 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[288],
                        m32 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[289],
                        m33 = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[290]
                    },
                    parentId = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[47],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[48],
                    m_Speed = -Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[291],
                    m_Acceleration = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.F32[292],
                    m_ConnectionID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[49],
                    m_SourceID = Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs.Values.I32[50]
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    }
}

// Embedded numerical arrays: Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs
internal static class Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs
{
    static Numeric_Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw01_PedestrianDraw01MechanismData_g_cs");
}
