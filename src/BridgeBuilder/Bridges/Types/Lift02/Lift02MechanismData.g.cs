// Generated from the game 1.6.2f1 native LiftBridge02 records.
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
internal static class Lift02MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[0];
        pillar.m_AnchorOffset = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[0];
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[1], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[2]);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[]
        {
            (NetPieceRequirements)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[1]
        };
        net.m_RequireRoad = (Game.Net.RoadTypes)(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[2]);
        net.m_RoadPassThrough = (Game.Net.RoadTypes)(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[3]);
        net.m_TrackPassThrough = (Game.Net.TrackTypes)(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[4]);
        net.m_NodeOffset = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[3];
        net.m_Attached = false;
        net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[4], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[5], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[6]);
        movement.m_MovingTime = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[7];
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[8], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[9], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[10]);
        standing.m_LegGap = new float2(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[11], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[12]);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[5])
            throw new ArgumentException("Lift02 requires its main, pillar and pillar-base render graphs.");
        return new[]
        {
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[6]],
                m_Position = new float3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[13], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[14], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[15]),
                m_Rotation = new quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[16], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[17], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[18], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[19]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[7])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[8]],
                m_Position = new float3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[20], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[21], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[22]),
                m_Rotation = new quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[23], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[24], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[25], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[26]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[9])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[10]],
                m_Position = new float3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[27], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[28], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[29]),
                m_Rotation = new quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[30], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[31], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[32], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[33]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[11])
            },
        };
    }

    // Full authored evidence. The generator replaces ordinary road attachments with
    // selected-road placements and widens structural attachments using measured plans.
    internal static ObjectSubObjectInfo[] AuthoredAttachments(Func<string, ObjectPrefab> resolve) => new ObjectSubObjectInfo[]
    {
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
                    name = "LiftBridge02_Root",
                    position = new Vector3(-Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[34], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[35], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[36]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[37], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[38], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[39], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[40]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[41], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[42], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[43]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[44],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[45],
                        m02 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[46],
                        m03 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[47],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[48],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[49],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[50],
                        m13 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[51],
                        m20 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[52],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[53],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[54],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[55],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[56],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[57],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[58],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[59]
                    },
                    parentId = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[12],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[13],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[60],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[61],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[14],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[15]
                },
                new()
                {
                    name = "LiftBridge02_TrainTrack",
                    position = new Vector3(-Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[62], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[63], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[64]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[65], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[66], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[67], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[68]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[69], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[70], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[71]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[72],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[73],
                        m02 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[74],
                        m03 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[75],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[76],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[77],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[78],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[79],
                        m20 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[80],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[81],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[82],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[83],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[84],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[85],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[86],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[87]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[16],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[17],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[88],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[89],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[18],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[19]
                },
                new()
                {
                    name = "LiftBridge02_WheelR",
                    position = new Vector3(-Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[90], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[91], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[92]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[93], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[94], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[95], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[96]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[97], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[98], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[99]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[100],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[101],
                        m02 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[102],
                        m03 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[103],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[104],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[105],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[106],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[107],
                        m20 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[108],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[109],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[110],
                        m23 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[111],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[112],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[113],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[114],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[115]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[20],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[21],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[116],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[117],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[22],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[23]
                },
                new()
                {
                    name = "LiftBridge02_WheelL",
                    position = new Vector3(-Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[118], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[119], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[120]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[121], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[122], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[123], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[124]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[125], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[126], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[127]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[128],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[129],
                        m02 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[130],
                        m03 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[131],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[132],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[133],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[134],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[135],
                        m20 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[136],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[137],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[138],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[139],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[140],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[141],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[142],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[143]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[24],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[25],
                    m_Speed = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[144],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[145],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[26],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[27]
                },
                new()
                {
                    name = "LiftBridge02_Weights",
                    position = new Vector3(-Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[146], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[147], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[148]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[149], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[150], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[151], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[152]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[153], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[154], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[155]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[156],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[157],
                        m02 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[158],
                        m03 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[159],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[160],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[161],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[162],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[163],
                        m20 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[164],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[165],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[166],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[167],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[168],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[169],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[170],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[171]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[28],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[29],
                    m_Speed = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[172],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[173],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[30],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[31]
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge02_Root",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[174], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[175], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[176]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[177], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[178], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[179], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[180]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[181], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[182], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[183]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[184],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[185],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[186],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[187],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[188],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[189],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[190],
                        m13 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[191],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[192],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[193],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[194],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[195],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[196],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[197],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[198],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[199]
                    },
                    parentId = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[32],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[33],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[200],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[201],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[34],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[35]
                },
                new()
                {
                    name = "LiftBridge02_TrainTrack",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[202], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[203], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[204]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[205], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[206], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[207], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[208]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[209], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[210], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[211]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[212],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[213],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[214],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[215],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[216],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[217],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[218],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[219],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[220],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[221],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[222],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[223],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[224],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[225],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[226],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[227]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[36],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[37],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[228],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[229],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[38],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[39]
                },
                new()
                {
                    name = "LiftBridge02_WheelR",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[230], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[231], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[232]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[233], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[234], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[235], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[236]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[237], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[238], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[239]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[240],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[241],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[242],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[243],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[244],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[245],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[246],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[247],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[248],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[249],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[250],
                        m23 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[251],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[252],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[253],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[254],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[255]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[40],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[41],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[256],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[257],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[42],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[43]
                },
                new()
                {
                    name = "LiftBridge02_WheelL",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[258], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[259], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[260]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[261], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[262], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[263], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[264]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[265], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[266], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[267]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[268],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[269],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[270],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[271],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[272],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[273],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[274],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[275],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[276],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[277],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[278],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[279],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[280],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[281],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[282],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[283]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[44],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[45],
                    m_Speed = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[284],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[285],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[46],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[47]
                },
                new()
                {
                    name = "LiftBridge02_Weights",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[286], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[287], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[288]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[289], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[290], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[291], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[292]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[293], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[294], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[295]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[296],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[297],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[298],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[299],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[300],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[301],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[302],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[303],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[304],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[305],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[306],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[307],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[308],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[309],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[310],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[311]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[48],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[49],
                    m_Speed = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[312],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[313],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[50],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[51]
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge02_Root",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[314], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[315], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[316]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[317], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[318], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[319], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[320]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[321], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[322], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[323]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[324],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[325],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[326],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[327],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[328],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[329],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[330],
                        m13 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[331],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[332],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[333],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[334],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[335],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[336],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[337],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[338],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[339]
                    },
                    parentId = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[52],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[53],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[340],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[341],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[54],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[55]
                },
                new()
                {
                    name = "LiftBridge02_TrainTrack",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[342], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[343], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[344]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[345], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[346], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[347], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[348]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[349], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[350], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[351]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[352],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[353],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[354],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[355],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[356],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[357],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[358],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[359],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[360],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[361],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[362],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[363],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[364],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[365],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[366],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[367]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[56],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[57],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[368],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[369],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[58],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[59]
                },
                new()
                {
                    name = "LiftBridge02_WheelR",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[370], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[371], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[372]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[373], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[374], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[375], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[376]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[377], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[378], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[379]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[380],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[381],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[382],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[383],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[384],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[385],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[386],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[387],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[388],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[389],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[390],
                        m23 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[391],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[392],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[393],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[394],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[395]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[60],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[61],
                    m_Speed = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[396],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[397],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[62],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[63]
                },
                new()
                {
                    name = "LiftBridge02_WheelL",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[398], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[399], -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[400]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[401], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[402], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[403], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[404]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[405], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[406], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[407]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[408],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[409],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[410],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[411],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[412],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[413],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[414],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[415],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[416],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[417],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[418],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[419],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[420],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[421],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[422],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[423]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[64],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[65],
                    m_Speed = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[424],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[425],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[66],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[67]
                },
                new()
                {
                    name = "LiftBridge02_Weights",
                    position = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[426], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[427], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[428]),
                    rotation = new Quaternion(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[429], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[430], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[431], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[432]),
                    scale = new Vector3(Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[433], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[434], Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[435]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[436],
                        m01 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[437],
                        m02 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[438],
                        m03 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[439],
                        m10 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[440],
                        m11 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[441],
                        m12 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[442],
                        m13 = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[443],
                        m20 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[444],
                        m21 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[445],
                        m22 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[446],
                        m23 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[447],
                        m30 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[448],
                        m31 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[449],
                        m32 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[450],
                        m33 = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[451]
                    },
                    parentId = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[68],
                    m_Type = (BoneType)Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[69],
                    m_Speed = -Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[452],
                    m_Acceleration = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.F32[453],
                    m_ConnectionID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[70],
                    m_SourceID = Numeric_Types_Lift02_Lift02MechanismData_g_cs.Values.I32[71]
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02MechanismData_g_cs
internal static class Numeric_Types_Lift02_Lift02MechanismData_g_cs
{
    static Numeric_Types_Lift02_Lift02MechanismData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02MechanismData_g_cs");
}
