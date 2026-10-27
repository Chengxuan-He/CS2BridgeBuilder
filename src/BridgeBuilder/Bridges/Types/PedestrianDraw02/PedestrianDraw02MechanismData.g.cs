// Generated from the game 1.6.2f1 native PedestrianDrawBridge02 records.
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
internal static class PedestrianDraw02MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[0];
        pillar.m_AnchorOffset = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[0];
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[1], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[2]);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[]
        {
            (NetPieceRequirements)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[1]
        };
        net.m_RequireRoad = (Game.Net.RoadTypes)(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[2]);
        net.m_RoadPassThrough = (Game.Net.RoadTypes)(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[3]);
        net.m_TrackPassThrough = (Game.Net.TrackTypes)(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[4]);
        net.m_NodeOffset = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[3];
        net.m_Attached = false;
        net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[4], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[5], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[6]);
        movement.m_MovingTime = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[7];
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[8], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[9], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[10]);
        standing.m_LegGap = new float2(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[11], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[12]);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[5])
            throw new ArgumentException("PedestrianDraw02 requires its main, pillar and pillar-base render graphs.");
        return new[]
        {
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[6]],
                m_Position = new float3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[13], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[14], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[15]),
                m_Rotation = new quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[16], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[17], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[18], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[19]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[7])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[8]],
                m_Position = new float3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[20], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[21], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[22]),
                m_Rotation = new quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[23], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[24], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[25], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[26]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[9])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[10]],
                m_Position = new float3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[27], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[28], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[29]),
                m_Rotation = new quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[30], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[31], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[32], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[33]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[11])
            },
        };
    }

    // Full authored evidence. The generator replaces ordinary road attachments with
    // selected-road placements and widens structural attachments using measured plans.
    internal static ObjectSubObjectInfo[] AuthoredAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof04"),
            m_Position = new float3(-Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[34], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[35], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[36]),
            m_Rotation = new quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[37], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[38], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[39], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[40]),
            m_ParentMesh = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[12],
            m_GroupIndex = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[13],
            m_Probability = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[14]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentRoof04"),
            m_Position = new float3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[41], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[42], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[43]),
            m_Rotation = new quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[44], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[45], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[46], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[47]),
            m_ParentMesh = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[15],
            m_GroupIndex = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[16],
            m_Probability = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[17]
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
                    name = "PedestrianDrawBridge02_Root",
                    position = new Vector3(-Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[48], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[49], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[50]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[51], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[52], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[53], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[54]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[55], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[56], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[57]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[58],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[59],
                        m02 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[60],
                        m03 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[61],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[62],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[63],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[64],
                        m13 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[65],
                        m20 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[66],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[67],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[68],
                        m23 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[69],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[70],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[71],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[72],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[73]
                    },
                    parentId = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[18],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[19],
                    m_Speed = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[74],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[75],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[20],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[21]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_Left",
                    position = new Vector3(-Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[76], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[77], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[78]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[79], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[80], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[81], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[82]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[83], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[84], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[85]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[86],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[87],
                        m02 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[88],
                        m03 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[89],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[90],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[91],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[92],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[93],
                        m20 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[94],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[95],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[96],
                        m23 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[97],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[98],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[99],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[100],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[101]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[22],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[23],
                    m_Speed = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[102],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[103],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[24],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[25]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_Right",
                    position = new Vector3(-Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[104], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[105], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[106]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[107], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[108], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[109], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[110]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[111], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[112], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[113]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[114],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[115],
                        m02 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[116],
                        m03 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[117],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[118],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[119],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[120],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[121],
                        m20 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[122],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[123],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[124],
                        m23 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[125],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[126],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[127],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[128],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[129]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[26],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[27],
                    m_Speed = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[130],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[131],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[28],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[29]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_LeftUpper",
                    position = new Vector3(-Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[132], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[133], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[134]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[135], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[136], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[137], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[138]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[139], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[140], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[141]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[142],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[143],
                        m02 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[144],
                        m03 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[145],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[146],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[147],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[148],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[149],
                        m20 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[150],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[151],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[152],
                        m23 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[153],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[154],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[155],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[156],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[157]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[30],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[31],
                    m_Speed = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[158],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[159],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[32],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[33]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_RightUpper",
                    position = new Vector3(-Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[160], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[161], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[162]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[163], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[164], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[165], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[166]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[167], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[168], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[169]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[170],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[171],
                        m02 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[172],
                        m03 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[173],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[174],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[175],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[176],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[177],
                        m20 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[178],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[179],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[180],
                        m23 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[181],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[182],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[183],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[184],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[185]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[34],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[35],
                    m_Speed = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[186],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[187],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[36],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[37]
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "PedestrianDrawBridge02_Root",
                    position = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[188], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[189], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[190]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[191], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[192], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[193], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[194]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[195], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[196], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[197]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[198],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[199],
                        m02 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[200],
                        m03 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[201],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[202],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[203],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[204],
                        m13 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[205],
                        m20 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[206],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[207],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[208],
                        m23 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[209],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[210],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[211],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[212],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[213]
                    },
                    parentId = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[38],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[39],
                    m_Speed = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[214],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[215],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[40],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[41]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_Left",
                    position = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[216], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[217], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[218]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[219], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[220], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[221], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[222]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[223], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[224], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[225]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[226],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[227],
                        m02 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[228],
                        m03 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[229],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[230],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[231],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[232],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[233],
                        m20 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[234],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[235],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[236],
                        m23 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[237],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[238],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[239],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[240],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[241]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[42],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[43],
                    m_Speed = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[242],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[243],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[44],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[45]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_Right",
                    position = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[244], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[245], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[246]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[247], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[248], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[249], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[250]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[251], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[252], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[253]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[254],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[255],
                        m02 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[256],
                        m03 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[257],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[258],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[259],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[260],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[261],
                        m20 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[262],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[263],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[264],
                        m23 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[265],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[266],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[267],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[268],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[269]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[46],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[47],
                    m_Speed = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[270],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[271],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[48],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[49]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_LeftUpper",
                    position = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[272], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[273], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[274]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[275], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[276], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[277], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[278]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[279], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[280], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[281]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[282],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[283],
                        m02 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[284],
                        m03 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[285],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[286],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[287],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[288],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[289],
                        m20 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[290],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[291],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[292],
                        m23 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[293],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[294],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[295],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[296],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[297]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[50],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[51],
                    m_Speed = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[298],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[299],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[52],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[53]
                },
                new()
                {
                    name = "PedestrianDrawBridge02_RightUpper",
                    position = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[300], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[301], -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[302]),
                    rotation = new Quaternion(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[303], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[304], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[305], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[306]),
                    scale = new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[307], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[308], Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[309]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[310],
                        m01 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[311],
                        m02 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[312],
                        m03 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[313],
                        m10 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[314],
                        m11 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[315],
                        m12 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[316],
                        m13 = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[317],
                        m20 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[318],
                        m21 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[319],
                        m22 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[320],
                        m23 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[321],
                        m30 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[322],
                        m31 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[323],
                        m32 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[324],
                        m33 = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[325]
                    },
                    parentId = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[54],
                    m_Type = (BoneType)Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[55],
                    m_Speed = -Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[326],
                    m_Acceleration = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.F32[327],
                    m_ConnectionID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[56],
                    m_SourceID = Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs.Values.I32[57]
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02MechanismData_g_cs");
}
