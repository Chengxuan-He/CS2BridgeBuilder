// Generated from the game 1.6.2f1 native LiftBridge04 records.
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
internal static class Lift04MechanismData
{
    internal static void ApplyComponents(StaticObjectPrefab target, ContentPrefab content)
    {
        var pillar = target.AddComponent<PillarObject>();
        pillar.m_Type = (PillarType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[0];
        pillar.m_AnchorOffset = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[0];
        pillar.m_VerticalPillarOffsetRange = new Bounds1(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[1], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[2]);
        var net = target.AddComponent<NetObject>();
        net.m_SetCompositionState = new[]
        {
            (NetPieceRequirements)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[1]
        };
        net.m_RequireRoad = (Game.Net.RoadTypes)(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[2]);
        net.m_RoadPassThrough = (Game.Net.RoadTypes)(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[3]);
        net.m_TrackPassThrough = (Game.Net.TrackTypes)(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[4]);
        net.m_NodeOffset = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[3];
        net.m_Attached = false;
        net.m_RequirePedestrian = false;
        var movement = target.AddComponent<MoveableBridge>();
        movement.m_LiftOffsets = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[4], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[5], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[6]);
        movement.m_MovingTime = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[7];
        target.AddComponent<ObjectSubObjects>().m_SubObjects = Array.Empty<ObjectSubObjectInfo>();
        target.AddComponent<ContentPrerequisite>().m_ContentPrerequisite = content;
        var standing = target.AddComponent<StandingObject>();
        standing.m_LegSize = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[8], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[9], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[10]);
        standing.m_LegGap = new float2(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[11], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[12]);
        standing.m_CircularLeg = false;
    }

    internal static ObjectMeshInfo[] Meshes(RenderPrefab[] prepared)
    {
        if (prepared.Length != Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[5])
            throw new ArgumentException("Lift04 requires its main, pillar and pillar-base render graphs.");
        return new[]
        {
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[6]],
                m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[13], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[14], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[15]),
                m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[16], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[17], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[18], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[19]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[7])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[8]],
                m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[20], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[21], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[22]),
                m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[23], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[24], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[25], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[26]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[9])
            },
            new ObjectMeshInfo
            {
                m_Mesh = prepared[Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[10]],
                m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[27], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[28], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[29]),
                m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[30], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[31], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[32], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[33]),
                m_RequireState = (Game.Objects.ObjectState)(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[11])
            },
        };
    }

    // Full authored evidence. The generator replaces ordinary road attachments with
    // selected-road placements and widens structural attachments using measured plans.
    internal static ObjectSubObjectInfo[] AuthoredAttachments(Func<string, ObjectPrefab> resolve) => new[]
    {
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[34], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[35], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[36]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[37], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[38], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[39], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[40]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[12],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[13],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[14]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("WallLightShortRange01"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[41], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[42], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[43]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[44], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[45], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[46], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[47]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[15],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[16],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[17]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentWall04"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[48], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[49], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[50]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[51], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[52], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[53], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[54]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[18],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[19],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[20]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("ACVentWall04"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[55], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[56], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[57]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[58], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[59], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[60], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[61]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[21],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[22],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[23]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[62], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[63], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[64]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[65], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[66], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[67], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[68]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[24],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[25],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[26]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[69], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[70], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[71]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[72], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[73], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[74], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[75]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[27],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[28],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[29]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[76], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[77], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[78]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[79], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[80], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[81], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[82]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[30],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[31],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[32]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[83], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[84], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[85]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[86], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[87], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[88], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[89]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[33],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[34],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[35]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[90], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[91], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[92]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[93], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[94], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[95], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[96]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[36],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[37],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[38]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[97], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[98], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[99]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[100], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[101], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[102], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[103]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[39],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[40],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[41]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[104], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[105], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[106]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[107], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[108], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[109], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[110]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[42],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[43],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[44]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[111], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[112], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[113]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[114], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[115], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[116], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[117]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[45],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[46],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[47]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[118], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[119], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[120]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[121], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[122], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[123], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[124]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[48],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[49],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[50]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[125], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[126], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[127]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[128], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[129], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[130], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[131]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[51],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[52],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[53]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[132], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[133], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[134]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[135], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[136], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[137], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[138]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[54],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[55],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[56]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[139], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[140], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[141]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[142], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[143], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[144], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[145]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[57],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[58],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[59]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[146], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[147], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[148]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[149], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[150], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[151], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[152]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[60],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[61],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[62]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[153], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[154], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[155]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[156], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[157], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[158], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[159]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[63],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[64],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[65]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[160], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[161], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[162]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[163], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[164], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[165], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[166]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[66],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[67],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[68]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[167], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[168], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[169]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[170], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[171], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[172], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[173]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[69],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[70],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[71]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[174], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[175], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[176]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[177], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[178], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[179], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[180]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[72],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[73],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[74]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[181], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[182], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[183]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[184], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[185], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[186], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[187]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[75],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[76],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[77]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[188], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[189], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[190]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[191], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[192], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[193], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[194]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[78],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[79],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[80]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[195], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[196], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[197]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[198], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[199], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[200], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[201]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[81],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[82],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[83]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[202], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[203], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[204]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[205], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[206], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[207], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[208]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[84],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[85],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[86]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[209], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[210], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[211]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[212], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[213], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[214], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[215]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[87],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[88],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[89]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[216], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[217], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[218]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[219], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[220], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[221], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[222]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[90],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[91],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[92]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[223], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[224], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[225]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[226], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[227], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[228], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[229]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[93],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[94],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[95]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[230], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[231], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[232]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[233], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[234], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[235], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[236]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[96],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[97],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[98]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[237], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[238], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[239]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[240], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[241], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[242], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[243]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[99],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[100],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[101]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[244], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[245], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[246]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[247], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[248], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[249], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[250]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[102],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[103],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[104]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[251], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[252], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[253]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[254], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[255], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[256], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[257]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[105],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[106],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[107]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[258], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[259], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[260]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[261], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[262], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[263], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[264]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[108],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[109],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[110]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[265], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[266], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[267]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[268], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[269], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[270], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[271]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[111],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[112],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[113]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[272], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[273], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[274]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[275], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[276], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[277], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[278]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[114],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[115],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[116]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[279], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[280], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[281]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[282], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[283], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[284], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[285]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[117],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[118],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[119]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[286], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[287], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[288]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[289], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[290], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[291], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[292]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[120],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[121],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[122]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[293], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[294], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[295]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[296], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[297], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[298], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[299]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[123],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[124],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[125]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[300], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[301], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[302]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[303], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[304], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[305], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[306]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[126],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[127],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[128]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[307], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[308], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[309]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[310], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[311], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[312], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[313]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[129],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[130],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[131]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[314], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[315], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[316]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[317], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[318], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[319], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[320]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[132],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[133],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[134]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[321], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[322], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[323]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[324], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[325], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[326], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[327]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[135],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[136],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[137]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[328], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[329], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[330]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[331], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[332], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[333], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[334]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[138],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[139],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[140]
        },
        new ObjectSubObjectInfo
        {
            m_Object = resolve("LadderMiddle02"),
            m_Position = new float3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[335], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[336], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[337]),
            m_Rotation = new quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[338], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[339], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[340], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[341]),
            m_ParentMesh = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[141],
            m_GroupIndex = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[142],
            m_Probability = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[143]
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
                    name = "LiftBridge02_Root",
                    position = new Vector3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[342], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[343], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[344]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[345], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[346], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[347], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[348]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[349], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[350], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[351]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[352],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[353],
                        m02 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[354],
                        m03 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[355],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[356],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[357],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[358],
                        m13 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[359],
                        m20 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[360],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[361],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[362],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[363],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[364],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[365],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[366],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[367]
                    },
                    parentId = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[144],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[145],
                    m_Speed = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[368],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[369],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[146],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[147]
                },
                new()
                {
                    name = "LiftBridge02_Weights",
                    position = new Vector3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[370], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[371], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[372]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[373], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[374], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[375], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[376]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[377], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[378], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[379]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[380],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[381],
                        m02 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[382],
                        m03 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[383],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[384],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[385],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[386],
                        m13 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[387],
                        m20 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[388],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[389],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[390],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[391],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[392],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[393],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[394],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[395]
                    },
                    parentId = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[148],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[149],
                    m_Speed = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[396],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[397],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[150],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[151]
                },
                new()
                {
                    name = "LiftBridge02_TrainTracks",
                    position = new Vector3(-Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[398], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[399], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[400]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[401], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[402], -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[403], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[404]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[405], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[406], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[407]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[408],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[409],
                        m02 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[410],
                        m03 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[411],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[412],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[413],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[414],
                        m13 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[415],
                        m20 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[416],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[417],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[418],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[419],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[420],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[421],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[422],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[423]
                    },
                    parentId = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[152],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[153],
                    m_Speed = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[424],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[425],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[154],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[155]
                },
            },
            1 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge02_Root",
                    position = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[426], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[427], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[428]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[429], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[430], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[431], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[432]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[433], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[434], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[435]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[436],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[437],
                        m02 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[438],
                        m03 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[439],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[440],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[441],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[442],
                        m13 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[443],
                        m20 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[444],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[445],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[446],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[447],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[448],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[449],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[450],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[451]
                    },
                    parentId = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[156],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[157],
                    m_Speed = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[452],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[453],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[158],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[159]
                },
                new()
                {
                    name = "LiftBridge02_Weights",
                    position = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[454], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[455], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[456]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[457], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[458], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[459], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[460]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[461], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[462], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[463]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[464],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[465],
                        m02 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[466],
                        m03 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[467],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[468],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[469],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[470],
                        m13 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[471],
                        m20 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[472],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[473],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[474],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[475],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[476],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[477],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[478],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[479]
                    },
                    parentId = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[160],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[161],
                    m_Speed = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[480],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[481],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[162],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[163]
                },
                new()
                {
                    name = "LiftBridge02_TrainTracks",
                    position = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[482], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[483], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[484]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[485], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[486], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[487], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[488]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[489], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[490], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[491]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[492],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[493],
                        m02 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[494],
                        m03 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[495],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[496],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[497],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[498],
                        m13 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[499],
                        m20 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[500],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[501],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[502],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[503],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[504],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[505],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[506],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[507]
                    },
                    parentId = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[164],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[165],
                    m_Speed = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[508],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[509],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[166],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[167]
                },
            },
            2 => new ProceduralAnimationProperties.BoneInfo[]
            {
                new()
                {
                    name = "LiftBridge02_Root",
                    position = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[510], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[511], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[512]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[513], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[514], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[515], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[516]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[517], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[518], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[519]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[520],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[521],
                        m02 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[522],
                        m03 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[523],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[524],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[525],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[526],
                        m13 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[527],
                        m20 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[528],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[529],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[530],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[531],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[532],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[533],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[534],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[535]
                    },
                    parentId = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[168],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[169],
                    m_Speed = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[536],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[537],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[170],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[171]
                },
                new()
                {
                    name = "LiftBridge02_Weights",
                    position = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[538], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[539], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[540]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[541], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[542], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[543], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[544]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[545], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[546], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[547]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[548],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[549],
                        m02 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[550],
                        m03 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[551],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[552],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[553],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[554],
                        m13 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[555],
                        m20 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[556],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[557],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[558],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[559],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[560],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[561],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[562],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[563]
                    },
                    parentId = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[172],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[173],
                    m_Speed = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[564],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[565],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[174],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[175]
                },
                new()
                {
                    name = "LiftBridge02_TrainTracks",
                    position = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[566], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[567], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[568]),
                    rotation = new Quaternion(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[569], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[570], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[571], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[572]),
                    scale = new Vector3(Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[573], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[574], Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[575]),
                    bindPose = new Matrix4x4
                    {
                        m00 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[576],
                        m01 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[577],
                        m02 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[578],
                        m03 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[579],
                        m10 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[580],
                        m11 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[581],
                        m12 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[582],
                        m13 = -Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[583],
                        m20 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[584],
                        m21 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[585],
                        m22 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[586],
                        m23 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[587],
                        m30 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[588],
                        m31 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[589],
                        m32 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[590],
                        m33 = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[591]
                    },
                    parentId = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[176],
                    m_Type = (BoneType)Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[177],
                    m_Speed = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[592],
                    m_Acceleration = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.F32[593],
                    m_ConnectionID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[178],
                    m_SourceID = Numeric_Types_Lift04_Lift04MechanismData_g_cs.Values.I32[179]
                },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(lod))};
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04MechanismData_g_cs
internal static class Numeric_Types_Lift04_Lift04MechanismData_g_cs
{
    static Numeric_Types_Lift04_Lift04MechanismData_g_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04MechanismData_g_cs");
}
