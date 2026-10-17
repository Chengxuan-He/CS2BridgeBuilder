using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TrafficSignNeeds = BridgeBuilder.Bridges.MovableRoadObjectSelection.TrafficSignNeeds;

namespace BridgeBuilder.Bridges;

// Native 1.6.2f1 node sign needs. All entity lookups belong to the prepared lane world.
// Concrete prefab selection and persistent signal representation are separate stages.
[DisableAutoCreation]
internal sealed partial class MovableNodeSignNeeds : SystemBase
{
    private struct TrafficSignData
    {
        internal Game.Objects.Transform m_ObjectTransform;
        internal float2 m_ForwardDirection;
        internal TrafficSignNeeds m_TrafficSignNeeds;
    }
    private struct TargetLaneData
    {
        internal CarLaneFlags m_CarLaneFlags, m_AndCarLaneFlags;
        internal float2 m_SpeedLimit;
    }
    private readonly List<TrafficSignData> _signs = new();
    private readonly Dictionary<PathNode, TargetLaneData> _targets = new();
    private uint _forwardMask, _backwardMask;
    private bool m_LeftHandTraffic;
    internal int Count => _signs.Count;
    protected override void OnUpdate() { }
    private ComponentLookup<Game.Net.CarLane> m_CarLaneData;
    private ComponentLookup<Curve> m_CurveData;
    private ComponentLookup<Lane> m_LaneData;
    private ComponentLookup<LaneSignal> m_LaneSignalData;
    private ComponentLookup<MasterLane> m_MasterLaneData;
    private ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;
    private ComponentLookup<CarLaneData> m_PrefabCarLaneData;
    private ComponentLookup<PrefabRef> m_PrefabRefData;
    private ComponentLookup<EdgeLane> m_EdgeLaneData;
    private ComponentLookup<SlaveLane> m_SlaveLaneData;

    internal void Begin(bool leftHandTraffic)
    {
        _signs.Clear(); _targets.Clear(); _forwardMask = _backwardMask = 0;
        m_LeftHandTraffic = leftHandTraffic;
        m_CarLaneData = GetComponentLookup<Game.Net.CarLane>(true);
        m_CurveData = GetComponentLookup<Curve>(true);
        m_LaneData = GetComponentLookup<Lane>(true);
        m_LaneSignalData = GetComponentLookup<LaneSignal>(true);
        m_MasterLaneData = GetComponentLookup<MasterLane>(true);
        m_PedestrianLaneData = GetComponentLookup<Game.Net.PedestrianLane>(true);
        m_PrefabCarLaneData = GetComponentLookup<CarLaneData>(true);
        m_PrefabRefData = GetComponentLookup<PrefabRef>(true);
        m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
        m_SlaveLaneData = GetComponentLookup<SlaveLane>(true);
    }

    internal int Add(MovableRoadObjectPlacement.Candidate candidate, TrafficSignNeeds initial)
    {
        var index = _signs.Count;
        _signs.Add(new TrafficSignData { m_ObjectTransform = candidate.Object,
            m_ForwardDirection = math.normalizesafe(math.forward(candidate.Object.m_Rotation).xz),
            m_TrafficSignNeeds = initial });
        return index;
    }

    internal TrafficSignNeeds Contribution(NetCompositionData edge, NetCompositionData node,
        NetGeometryData network, float2 approachDirection, bool isEnd)
    {
        var needs = new TrafficSignNeeds { m_Left = LaneDirectionType.None,
            m_Forward = LaneDirectionType.None, m_Right = LaneDirectionType.None };
        if ((network.m_MergeLayers & (Layer.Road | Layer.TramTrack | Layer.PublicTransportRoad)) == Layer.None)
            return needs;
        var direction = math.normalizesafe(approachDirection);
        var angle = math.atan2(direction.x, direction.y);
        var bit = (uint)(1 << (Mathf.FloorToInt(angle * (16f / MathF.PI)) & 0x1F));
        if (((uint)edge.m_State & (uint)(isEnd ? 8 : 64)) != 0) _forwardMask |= bit;
        if (((uint)edge.m_State & (uint)(isEnd ? 64 : 8)) != 0) _backwardMask |= bit;
        if ((edge.m_State & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes)) ==
            (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes))
            needs.m_RemoveSignTypeMask2 = TrafficSignDataMask(TrafficSignType.DoNotEnter) | TrafficSignDataMask(TrafficSignType.Oneway);
        if ((node.m_Flags.m_General & CompositionFlags.General.Intersection) == 0)
            needs.m_RemoveSignTypeMask = TrafficSignDataMask(TrafficSignType.Street) |
                TrafficSignDataMask(TrafficSignType.NoTurnLeft) | TrafficSignDataMask(TrafficSignType.NoTurnRight) |
                TrafficSignDataMask(TrafficSignType.NoUTurnLeft) | TrafficSignDataMask(TrafficSignType.NoUTurnRight);
        if ((network.m_IntersectLayers & Layer.Waterway) == 0 ||
            (edge.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) == 0 ||
            (node.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) != 0)
            needs.m_RemoveSignTypeMask2 |= TrafficSignDataMask(TrafficSignType.MoveableBridge);
        return needs;
    }

    private static uint TrafficSignDataMask(TrafficSignType type) => Game.Prefabs.TrafficSignData.GetTypeMask(type);

    internal void CollectEdgeTargets(Entity owner, Entity edge, bool isEnd, int firstSign)
    {
        var lanes = EntityManager.GetBuffer<Game.Net.SubLane>(edge, true);
        for (var j = 0; j < lanes.Length; j++)
        {
            var subLane = lanes[j].m_SubLane;
            if (EntityManager.HasComponent<Game.Net.SecondaryLane>(subLane) ||
                EntityManager.HasComponent<Game.Net.UtilityLane>(subLane)) continue;
            if (!m_CarLaneData.HasComponent(subLane) || m_MasterLaneData.HasComponent(subLane) || !m_EdgeLaneData.HasComponent(subLane))
            {
            	continue;
            }
            Game.Net.CarLane carLane = m_CarLaneData[subLane];
            bool2 x5 = m_EdgeLaneData[subLane].m_EdgeDelta == math.select(0f, 1f, isEnd);
            if (!math.any(x5))
            {
            	continue;
            }
            Lane lane = m_LaneData[subLane];
            PathNode key = (x5.x ? lane.m_StartNode : lane.m_EndNode);
            if (x5.x && key.OwnerEquals(new PathNode(owner, 0)) && _signs.Count > firstSign)
            {
            	Curve curve5 = m_CurveData[subLane];
            	int num20 = -1;
            	float num21 = float.MaxValue;
            	float2 float6 = math.normalizesafe(MathUtils.StartTangent(curve5.m_Bezier).xz);
            	float3 a = curve5.m_Bezier.a;
            	a.xz += MathUtils.Right(float6) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
            	for (int l = firstSign; l < _signs.Count; l++)
            	{
            		TrafficSignData trafficSignData = _signs[l];
            		if (math.dot(float6, trafficSignData.m_ForwardDirection) > 0.70710677f)
            		{
            			float num22 = math.distance(a, trafficSignData.m_ObjectTransform.m_Position);
            			if (num22 < num21)
            			{
            				num20 = l;
            				num21 = num22;
            			}
            		}
            	}
            	if (num20 != -1)
            	{
            		TrafficSignData value8 = _signs[num20];
            		value8.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.MoveableBridge);
            		_signs[num20] = value8;
            	}
            }
            if (!key.OwnerEquals(new PathNode(edge, 0)))
            {
            	continue;
            }

            CarLaneFlags carLaneFlags = carLane.m_Flags;
            if (m_SlaveLaneData.TryGetComponent(subLane, out var componentData))
            {
            	for (int m = componentData.m_MinIndex; m <= componentData.m_MaxIndex; m++)
            	{
            		if (m != j && m_CarLaneData.TryGetComponent(lanes[m].m_SubLane, out var componentData2))
            		{
            			carLaneFlags |= componentData2.m_Flags;
            		}
            	}
            }
            TargetLaneData item = new TargetLaneData
            {
            	m_CarLaneFlags = carLane.m_Flags,
            	m_AndCarLaneFlags = carLaneFlags,
            	m_SpeedLimit = carLane.m_DefaultSpeedLimit
            };
            if (!_targets.TryAdd(key, item))
            {
            	UnityEngine.Debug.Log($"SecondaryObjectSystem: Duplicate node for lane {subLane.Index}");
            }
        }
    }

    internal TrafficSignNeeds[] Calculate(Entity node)
    {
        var lanes = EntityManager.GetBuffer<Game.Net.SubLane>(node, true);
        for (var index = 0; index < lanes.Length; index++)
        {
            var subLane2 = lanes[index].m_SubLane;
            if (_signs.Count == 0 || EntityManager.HasComponent<Game.Net.SecondaryLane>(subLane2) ||
                EntityManager.HasComponent<Game.Net.UtilityLane>(subLane2)) continue;
            if (m_CarLaneData.HasComponent(subLane2))
            {
            	if (m_MasterLaneData.HasComponent(subLane2))
            	{
            		continue;
            	}
            	Game.Net.CarLane carLane2 = m_CarLaneData[subLane2];
            	Lane lane2 = m_LaneData[subLane2];
            	Curve curve7 = m_CurveData[subLane2];
            	PrefabRef prefabRef5 = m_PrefabRefData[subLane2];
            	bool flag5 = (_forwardMask | _backwardMask) != 0;
            	if (flag5 && m_PrefabCarLaneData.TryGetComponent(prefabRef5.m_Prefab, out var componentData5))
            	{
            		flag5 = componentData5.m_RoadTypes == RoadTypes.Bicycle;
            	}
            	if ((carLane2.m_Flags & CarLaneFlags.Unsafe) != 0)
            	{
            		carLane2.m_Flags &= CarLaneFlags.Unsafe | CarLaneFlags.Highway | CarLaneFlags.RightLimit | CarLaneFlags.LeftLimit;
            	}
            	bool flag6 = (carLane2.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.Yield | CarLaneFlags.Stop | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight | CarLaneFlags.RightLimit | CarLaneFlags.LeftLimit)) != 0;
            	bool flag7 = m_LaneSignalData.HasComponent(subLane2);
            	int num40 = -1;
            	int num41 = -1;
            	int num42 = -1;
            	int num43 = -1;
            	float num44 = float.MaxValue;
            	float num45 = float.MaxValue;
            	float num46 = float.MaxValue;
            	float num47 = float.MaxValue;
            	float2 float7 = math.normalizesafe(MathUtils.StartTangent(curve7.m_Bezier).xz);
            	float2 float8 = math.normalizesafe(MathUtils.EndTangent(curve7.m_Bezier).xz);
            	float3 a2 = curve7.m_Bezier.a;
            	float3 a3 = curve7.m_Bezier.a;
            	float3 d = curve7.m_Bezier.d;
            	float3 d2 = curve7.m_Bezier.d;
            	a2.xz += MathUtils.Right(float7) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
            	a3.xz += MathUtils.Left(float7) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
            	d.xz += MathUtils.Right(float8) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
            	d2.xz += MathUtils.Left(float8) * math.select(1.25f, -1.25f, m_LeftHandTraffic);
            	for (int num48 = 0; num48 < _signs.Count; num48++)
            	{
            		TrafficSignData trafficSignData2 = _signs[num48];
            		float num49 = math.dot(float7, trafficSignData2.m_ForwardDirection);
            		float num50 = math.dot(float8, trafficSignData2.m_ForwardDirection);
            		if (num49 < -0.70710677f)
            		{
            			float num51 = math.distance(a2, trafficSignData2.m_ObjectTransform.m_Position);
            			float num52 = math.distance(a3, trafficSignData2.m_ObjectTransform.m_Position);
            			if (num51 < num44)
            			{
            				num40 = num48;
            				num44 = num51;
            			}
            			if (num52 < num45)
            			{
            				num41 = num48;
            				num45 = num52;
            			}
            		}
            		if (num50 > 0.70710677f)
            		{
            			float num53 = math.distance(d, trafficSignData2.m_ObjectTransform.m_Position);
            			float num54 = math.distance(d2, trafficSignData2.m_ObjectTransform.m_Position);
            			if (num53 < num46)
            			{
            				num42 = num48;
            				num46 = num53;
            			}
            			if (num54 < num47)
            			{
            				num43 = num48;
            				num47 = num54;
            			}
            		}
            	}
            	float num55 = math.atan2(float7.x, float7.y);
            	float num56 = math.atan2(float8.x, float8.y);
            	int num57 = Mathf.FloorToInt(num55 * (16f / MathF.PI)) & 0x1F;
            	int num58 = (Mathf.FloorToInt(num56 * (16f / MathF.PI)) + 16) & 0x1F;
            	if ((flag6 || flag7) && num40 != -1)
            	{
            		TrafficSignData value15 = _signs[num40];
            		if (flag6 && _targets.TryGetValue(lane2.m_StartNode, out var item2))
            		{
            			if ((carLane2.m_Flags & CarLaneFlags.Stop) != 0)
            			{
            				value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Stop);
            			}
            			if ((carLane2.m_Flags & item2.m_AndCarLaneFlags & CarLaneFlags.Yield) != 0)
            			{
            				value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Yield);
            			}
            			if (!flag5)
            			{
            				if ((carLane2.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.GentleTurnLeft)) != 0)
            				{
            					value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnLeft);
            				}
            				else if ((carLane2.m_Flags & CarLaneFlags.LeftLimit) != 0)
            				{
            					uint num59 = (uint)(2032 << num57) | math.select(2032u >> 32 - num57, 0u, num57 == 0);
            					if (((_forwardMask | _backwardMask) & num59) != 0)
            					{
            						value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnLeft);
            					}
            				}
            				if ((carLane2.m_Flags & CarLaneFlags.UTurnLeft) != 0)
            				{
            					value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnLeft);
            				}
            				else if ((carLane2.m_Flags & CarLaneFlags.LeftLimit) != 0)
            				{
            					uint num60 = (uint)(14 << num57) | math.select(14u >> 32 - num57, 0u, num57 == 0);
            					if (((_forwardMask | _backwardMask) & num60) != 0)
            					{
            						value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnLeft);
            					}
            				}
            				if ((carLane2.m_Flags & (CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnRight)) != 0)
            				{
            					value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnRight);
            				}
            				else if ((carLane2.m_Flags & CarLaneFlags.RightLimit) != 0)
            				{
            					uint num61 = (uint)(532676608 << num57) | math.select(532676608u >> 32 - num57, 0u, num57 == 0);
            					if (((_forwardMask | _backwardMask) & num61) != 0)
            					{
            						value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoTurnRight);
            					}
            				}
            				if ((carLane2.m_Flags & CarLaneFlags.UTurnRight) != 0)
            				{
            					value15.m_TrafficSignNeeds.m_RemoveSignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnRight);
            				}
            				else if ((carLane2.m_Flags & CarLaneFlags.RightLimit) != 0)
            				{
            					uint num62 = (uint)(-536870912 << num57) | math.select(3758096384u >> 32 - num57, 0u, num57 == 0);
            					if (((_forwardMask | _backwardMask) & num62) != 0)
            					{
            						value15.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoUTurnRight);
            					}
            				}
            			}
            		}
            		if (flag7)
            		{
            			LaneSignal laneSignal = m_LaneSignalData[subLane2];
            			float num63 = math.dot(MathUtils.Right(float7), value15.m_ObjectTransform.m_Position.xz - curve7.m_Bezier.a.xz);
            			if (num63 > 0f)
            			{
            				value15.m_TrafficSignNeeds.m_VehicleLanesRight = math.max(value15.m_TrafficSignNeeds.m_VehicleLanesRight, num63);
            			}
            			else
            			{
            				value15.m_TrafficSignNeeds.m_VehicleLanesLeft = math.max(value15.m_TrafficSignNeeds.m_VehicleLanesLeft, 0f - num63);
            			}
            			value15.m_TrafficSignNeeds.m_VehicleMask |= laneSignal.m_GroupMask;
            		}
            		_signs[num40] = value15;
            	}
            	if (flag6 && !flag5 && num41 != -1)
            	{
            		TrafficSignData value16 = _signs[num41];
            		if (flag6 && _targets.TryGetValue(lane2.m_StartNode, out var _))
            		{
            			if (m_LeftHandTraffic)
            			{
            				if ((carLane2.m_Flags & CarLaneFlags.RightLimit) != 0)
            				{
            					uint num64 = (uint)(536870896 << num57) | math.select(536870896u >> 32 - num57, 0u, num57 == 0);
            					if ((_forwardMask & num64) != 0)
            					{
            						value16.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.DoNotEnter);
            					}
            				}
            			}
            			else if ((carLane2.m_Flags & CarLaneFlags.LeftLimit) != 0)
            			{
            				uint num65 = (uint)(536870896 << num57) | math.select(536870896u >> 32 - num57, 0u, num57 == 0);
            				if ((_forwardMask & num65) != 0)
            				{
            					value16.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.DoNotEnter);
            				}
            			}
            		}
            		_signs[num41] = value16;
            	}
            	if (num42 != -1)
            	{
            		TrafficSignData value17 = _signs[num42];
            		if (_targets.TryGetValue(lane2.m_EndNode, out var item4))
            		{
            			if (m_LeftHandTraffic)
            			{
            				if ((item4.m_CarLaneFlags & (CarLaneFlags.Highway | CarLaneFlags.LeftLimit)) == CarLaneFlags.LeftLimit && (carLane2.m_Flags & CarLaneFlags.TurnLeft) != 0)
            				{
            					value17.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Street);
            				}
            			}
            			else if ((item4.m_CarLaneFlags & (CarLaneFlags.Highway | CarLaneFlags.RightLimit)) == CarLaneFlags.RightLimit && (carLane2.m_Flags & CarLaneFlags.TurnRight) != 0)
            			{
            				value17.m_TrafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Street);
            			}
            			if (!flag5)
            			{
            				uint num66 = (uint)(536870896 << num57) | math.select(536870896u >> 32 - num57, 0u, num57 == 0);
            				uint num67 = (uint)(536870896 << num58) | math.select(536870896u >> 32 - num58, 0u, num58 == 0);
            				if ((_forwardMask & num66) != 0 || (_backwardMask & num67) != 0)
            				{
            					value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Oneway);
            				}
            			}
            			if ((item4.m_CarLaneFlags & ~carLane2.m_Flags & CarLaneFlags.Highway) != 0)
            			{
            				value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Motorway);
            			}
            			if (math.abs(item4.m_SpeedLimit.x - carLane2.m_DefaultSpeedLimit) > 1f)
            			{
            				value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.SpeedLimit);
            				value17.m_TrafficSignNeeds.m_SpeedLimit2 = (ushort)Mathf.RoundToInt(item4.m_SpeedLimit.x * 3.6f);
            			}
            			value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.MoveableBridge);
            			CarLaneFlags carLaneFlags2 = (m_LeftHandTraffic ? CarLaneFlags.ParkingLeft : CarLaneFlags.ParkingRight);
            			if ((item4.m_CarLaneFlags & carLaneFlags2) != 0)
            			{
            				if (_targets.TryGetValue(lane2.m_StartNode, out var item5))
            				{
            					if ((item5.m_CarLaneFlags & carLaneFlags2) == 0 && (carLane2.m_Flags & CarLaneFlags.Unsafe) == 0)
            					{
            						value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
            					}
            				}
            				else
            				{
            					value17.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
            				}
            			}
            		}
            		_signs[num42] = value17;
            	}
            	if (num43 == -1)
            	{
            		continue;
            	}
            	TrafficSignData value18 = _signs[num43];
            	if (_targets.TryGetValue(lane2.m_EndNode, out var item6))
            	{
            		CarLaneFlags carLaneFlags3 = (m_LeftHandTraffic ? CarLaneFlags.ParkingRight : CarLaneFlags.ParkingLeft);
            		if ((item6.m_CarLaneFlags & carLaneFlags3) != 0)
            		{
            			if (_targets.TryGetValue(lane2.m_StartNode, out var item7))
            			{
            				if ((item7.m_CarLaneFlags & carLaneFlags3) == 0 && (carLane2.m_Flags & CarLaneFlags.Unsafe) == 0)
            				{
            					value18.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
            				}
            			}
            			else
            			{
            				value18.m_TrafficSignNeeds.m_SignTypeMask2 |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.Parking);
            			}
            		}
            	}
            	_signs[num43] = value18;
            }
            else
            {
            	if (!m_PedestrianLaneData.HasComponent(subLane2) || (m_PedestrianLaneData[subLane2].m_Flags & PedestrianLaneFlags.Crosswalk) == 0 || !m_LaneSignalData.HasComponent(subLane2))
            	{
            		continue;
            	}
            	LaneSignal laneSignal2 = m_LaneSignalData[subLane2];
            	Curve curve8 = m_CurveData[subLane2];
            	int num68 = -1;
            	int num69 = -1;
            	float num70 = float.MaxValue;
            	float num71 = float.MaxValue;
            	float2 float9 = math.normalizesafe(MathUtils.StartTangent(curve8.m_Bezier).xz);
            	float2 float10 = math.normalizesafe(MathUtils.EndTangent(curve8.m_Bezier).xz);
            	for (int num72 = 0; num72 < _signs.Count; num72++)
            	{
            		TrafficSignData trafficSignData3 = _signs[num72];
            		float num73 = 1f + math.distance(curve8.m_Bezier.a, trafficSignData3.m_ObjectTransform.m_Position);
            		float num74 = 1f + math.distance(curve8.m_Bezier.d, trafficSignData3.m_ObjectTransform.m_Position);
            		num73 *= 1f + math.abs(math.dot(float9, trafficSignData3.m_ForwardDirection));
            		num74 *= 1f + math.abs(math.dot(float10, trafficSignData3.m_ForwardDirection));
            		if (num73 < num70)
            		{
            			num68 = num72;
            			num70 = num73;
            		}
            		if (num74 < num71)
            		{
            			num69 = num72;
            			num71 = num74;
            		}
            	}
            	if (num68 != -1)
            	{
            		TrafficSignData value19 = _signs[num68];
            		if (math.dot(MathUtils.Right(float9), value19.m_ForwardDirection) > 0f)
            		{
            			value19.m_TrafficSignNeeds.m_CrossingLeftMask |= laneSignal2.m_GroupMask;
            		}
            		else
            		{
            			value19.m_TrafficSignNeeds.m_CrossingRightMask |= laneSignal2.m_GroupMask;
            		}
            		_signs[num68] = value19;
            	}
            	if (num69 != -1)
            	{
            		TrafficSignData value20 = _signs[num69];
            		if (math.dot(MathUtils.Right(float10), value20.m_ForwardDirection) > 0f)
            		{
            			value20.m_TrafficSignNeeds.m_CrossingRightMask |= laneSignal2.m_GroupMask;
            		}
            		else
            		{
            			value20.m_TrafficSignNeeds.m_CrossingLeftMask |= laneSignal2.m_GroupMask;
            		}
            		_signs[num69] = value20;
            	}
            }
        }
        var result = new TrafficSignNeeds[_signs.Count];
        for (var i = 0; i < result.Length; i++)
        {
            var needs = _signs[i].m_TrafficSignNeeds;
            needs.m_SignTypeMask &= ~needs.m_RemoveSignTypeMask;
            needs.m_SignTypeMask2 &= ~needs.m_RemoveSignTypeMask2;
            result[i] = needs;
        }
        return result;
    }

    internal static bool Requested(TrafficSignNeeds needs) => needs.m_SignTypeMask != 0 ||
        needs.m_SignTypeMask2 != 0 || needs.m_VehicleMask != 0 ||
        needs.m_CrossingLeftMask != 0 || needs.m_CrossingRightMask != 0;
}
