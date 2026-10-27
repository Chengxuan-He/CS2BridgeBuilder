// Generated from the game 1.6.2f1 native DrawBridge02 records.
// Source SHA-256: 47EF0F1723E8643E74A17751AA480F97260BF6E549AA60EC9F14F8E003B12070
// Native serialized matrix elements are row-major; audit labels came from managed field order.
// Bind-pose field indices below are transposed once; numeric payloads are unchanged.
// Resource path IDs are extraction provenance only; none is used at runtime.
using System;
using System.Linq;
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
        pillar.m_Type = (PillarType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[0];
        pillar.m_AnchorOffset = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[0];
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[1], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[2]);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[]
        {
            (NetPieceRequirements)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[1]
        };
        net.m_RequireRoad = (Game.Net.RoadTypes)(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[2]);
        net.m_RoadPassThrough = (Game.Net.RoadTypes)(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[3]);
        net.m_TrackPassThrough = (Game.Net.TrackTypes)(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[4]);
        net.m_NodeOffset = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[3];
        net.m_Attached = false;
        net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[4], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[5], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[6]);
        movement.m_MovingTime = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[7];
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[8], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[9], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[10]);
        standing.m_LegGap = new float2(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[11], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[12]);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[5])
            throw new ArgumentException("Draw02 requires its main, pillar and pillar-base render graphs.");
        return new[]
        {
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[6]],
                m_Position = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[13], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[14], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[15]),
                m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[16], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[17], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[18], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[19]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[7])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[8]],
                m_Position = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[20], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[21], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[22]),
                m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[23], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[24], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[25], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[26]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[9])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[10]],
                m_Position = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[27], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[28], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[29]),
                m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[30], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[31], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[32], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[33]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[11])
            },
        };
    }

    // Full authored evidence. The generator replaces ordinary road attachments with
    // selected-road placements and widens structural attachments using measured plans.
    internal static ObjectSubObjectInfo[] AuthoredAttachments(Func<string, ObjectPrefab> resolve) => RoadAttachments(resolve).Concat(StructuralAttachments(resolve)).ToArray();
    internal static ObjectSubObjectInfo[] RoadAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[34], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[35], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[36]),
            m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[37], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[38], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[39], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[40]),
            m_ParentMesh = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[12],
            m_GroupIndex = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[13],
            m_Probability = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[14]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[41], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[42], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[43]),
            m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[44], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[45], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[46], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[47]),
            m_ParentMesh = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[15],
            m_GroupIndex = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[16],
            m_Probability = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[17]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[48], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[49], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[50]),
            m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[51], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[52], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[53], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[54]),
            m_ParentMesh = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[18],
            m_GroupIndex = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[19],
            m_Probability = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[20]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("Street Light Single Placeholder"),
            m_Position = new float3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[55], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[56], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[57]),
            m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[58], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[59], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[60], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[61]),
            m_ParentMesh = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[21],
            m_GroupIndex = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[22],
            m_Probability = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[23]
        },
    };
    internal static ObjectSubObjectInfo[] StructuralAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentWall05"),
            m_Position = new float3(-Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[62], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[63], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[64]),
            m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[65], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[66], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[67], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[68]),
            m_ParentMesh = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[24],
            m_GroupIndex = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[25],
            m_Probability = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[26]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentWall05"),
            m_Position = new float3(-Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[69], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[70], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[71]),
            m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[72], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[73], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[74], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[75]),
            m_ParentMesh = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[27],
            m_GroupIndex = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[28],
            m_Probability = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[29]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange03"),
            m_Position = new float3(-Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[76], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[77], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[78]),
            m_Rotation = new quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[79], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[80], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[81], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[82]),
            m_ParentMesh = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[30],
            m_GroupIndex = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[31],
            m_Probability = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[32]
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
                    position = new Vector3(-Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[83], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[84], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[85]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[86], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[87], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[88], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[89]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[90], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[91], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[92]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[93],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[94],
                        m02 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[95],
                        m03 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[96],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[97],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[98],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[99],
                        m13 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[100],
                        m20 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[101],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[102],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[103],
                        m23 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[104],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[105],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[106],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[107],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[108]
                    },
                    parentId = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[33],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[34],
                    m_Speed = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[109],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[110],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[35],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[36]
                },
                new()
                {
                    name = "DrawBridge02_Left",
                    position = new Vector3(-Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[111], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[112], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[113]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[114], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[115], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[116], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[117]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[118], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[119], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[120]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[121],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[122],
                        m02 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[123],
                        m03 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[124],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[125],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[126],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[127],
                        m13 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[128],
                        m20 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[129],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[130],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[131],
                        m23 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[132],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[133],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[134],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[135],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[136]
                    },
                    parentId = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[37],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[38],
                    m_Speed = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[137],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[138],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[39],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[40]
                },
                new()
                {
                    name = "DrawBridge02_Right",
                    position = new Vector3(-Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[139], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[140], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[141]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[142], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[143], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[144], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[145]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[146], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[147], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[148]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[149],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[150],
                        m02 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[151],
                        m03 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[152],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[153],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[154],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[155],
                        m13 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[156],
                        m20 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[157],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[158],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[159],
                        m23 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[160],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[161],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[162],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[163],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[164]
                    },
                    parentId = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[41],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[42],
                    m_Speed = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[165],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[166],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[43],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[44]
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge02_Root",
                    position = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[167], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[168], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[169]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[170], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[171], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[172], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[173]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[174], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[175], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[176]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[177],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[178],
                        m02 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[179],
                        m03 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[180],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[181],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[182],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[183],
                        m13 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[184],
                        m20 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[185],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[186],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[187],
                        m23 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[188],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[189],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[190],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[191],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[192]
                    },
                    parentId = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[45],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[46],
                    m_Speed = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[193],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[194],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[47],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[48]
                },
                new()
                {
                    name = "DrawBridge02_Left",
                    position = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[195], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[196], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[197]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[198], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[199], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[200], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[201]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[202], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[203], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[204]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[205],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[206],
                        m02 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[207],
                        m03 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[208],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[209],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[210],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[211],
                        m13 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[212],
                        m20 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[213],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[214],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[215],
                        m23 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[216],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[217],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[218],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[219],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[220]
                    },
                    parentId = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[49],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[50],
                    m_Speed = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[221],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[222],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[51],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[52]
                },
                new()
                {
                    name = "DrawBridge02_Right",
                    position = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[223], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[224], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[225]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[226], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[227], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[228], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[229]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[230], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[231], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[232]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[233],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[234],
                        m02 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[235],
                        m03 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[236],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[237],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[238],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[239],
                        m13 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[240],
                        m20 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[241],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[242],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[243],
                        m23 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[244],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[245],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[246],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[247],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[248]
                    },
                    parentId = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[53],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[54],
                    m_Speed = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[249],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[250],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[55],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[56]
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "DrawBridge02_Root",
                    position = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[251], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[252], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[253]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[254], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[255], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[256], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[257]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[258], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[259], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[260]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[261],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[262],
                        m02 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[263],
                        m03 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[264],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[265],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[266],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[267],
                        m13 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[268],
                        m20 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[269],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[270],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[271],
                        m23 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[272],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[273],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[274],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[275],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[276]
                    },
                    parentId = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[57],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[58],
                    m_Speed = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[277],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[278],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[59],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[60]
                },
                new()
                {
                    name = "DrawBridge02_Left",
                    position = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[279], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[280], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[281]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[282], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[283], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[284], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[285]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[286], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[287], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[288]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[289],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[290],
                        m02 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[291],
                        m03 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[292],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[293],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[294],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[295],
                        m13 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[296],
                        m20 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[297],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[298],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[299],
                        m23 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[300],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[301],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[302],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[303],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[304]
                    },
                    parentId = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[61],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[62],
                    m_Speed = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[305],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[306],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[63],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[64]
                },
                new()
                {
                    name = "DrawBridge02_Right",
                    position = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[307], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[308], -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[309]),
                    rotation = new Quaternion(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[310], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[311], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[312], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[313]),
                    scale = new Vector3(Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[314], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[315], Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[316]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[317],
                        m01 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[318],
                        m02 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[319],
                        m03 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[320],
                        m10 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[321],
                        m11 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[322],
                        m12 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[323],
                        m13 = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[324],
                        m20 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[325],
                        m21 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[326],
                        m22 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[327],
                        m23 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[328],
                        m30 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[329],
                        m31 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[330],
                        m32 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[331],
                        m33 = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[332]
                    },
                    parentId = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[65],
                    m_Type = (BoneType)Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[66],
                    m_Speed = -Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[333],
                    m_Acceleration = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.F32[334],
                    m_ConnectionID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[67],
                    m_SourceID = Numeric_Types_Draw02_Draw02MechanismData_g_cs.Values.I32[68]
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02MechanismData_g_cs
internal static class Numeric_Types_Draw02_Draw02MechanismData_g_cs
{
    static Numeric_Types_Draw02_Draw02MechanismData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02MechanismData_g_cs");
}
