// Native edge traffic-needs calculation from game 1.6.2f1 SecondaryObjectSystem.
// Runs against an explicitly prepared lane world; it never constructs or modifies city networks.
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TrafficSignNeeds = BridgeBuilder.Bridges.MovableRoadObjectSelection.TrafficSignNeeds;

namespace BridgeBuilder.Bridges;

[DisableAutoCreation]
internal sealed partial class MovableRoadTrafficNeeds : SystemBase
{
    private struct TargetLaneData
    {
        internal CarLaneFlags m_CarLaneFlags, m_AndCarLaneFlags;
        internal float2 m_SpeedLimit;
    }
    private sealed class LaneContext
    {
        internal readonly Dictionary<PathNode, TargetLaneData> m_SourceLanes = new();
        internal readonly Dictionary<PathNode, TargetLaneData> m_TargetLanes = new();
    }
    private LaneContext _context = new();
    private Entity _owner;
    private bool _collected, m_LeftHandTraffic;
    private bool2 _forbidBicycles, _neighborForbidBicycles;
    protected override void OnUpdate() { }
    private ComponentLookup<BorderDistrict> m_BorderDistrictData;
    private ComponentLookup<Game.Net.CarLane> m_CarLaneData;
    private ComponentLookup<Curve> m_CurveData;
    private ComponentLookup<District> m_DistrictData;
    private ComponentLookup<EdgeLane> m_EdgeLaneData;
    private BufferLookup<ConnectedEdge> m_Edges;
    private ComponentLookup<Hidden> m_HiddenData;
    private ComponentLookup<Lane> m_LaneData;
    private ComponentLookup<MasterLane> m_MasterLaneData;
    private ComponentLookup<Composition> m_NetCompositionData;
    private ComponentLookup<Game.Net.Edge> m_NetEdgeData;
    private ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;
    private ComponentLookup<LaneDirectionData> m_PrefabLaneDirectionData;
    private ComponentLookup<NetCompositionData> m_PrefabNetCompositionData;
    private ComponentLookup<Game.Prefabs.TrafficSignData> m_PrefabTrafficSignData;
    private ComponentLookup<Game.Net.SecondaryLane> m_SecondaryLaneData;
    private ComponentLookup<SlaveLane> m_SlaveLaneData;
    private BufferLookup<Game.Net.SubLane> m_SubLanes;
    private ComponentLookup<Temp> m_TempData;

    // owner must include its actual edge endpoints and generated node/edge lanes. Do not pass
    // empty synthetic node buffers to imply that a connected road has no incoming traffic.
    internal void Begin(Entity owner, bool leftHandTraffic)
    {
        m_BorderDistrictData = GetComponentLookup<BorderDistrict>(true);
        m_CarLaneData = GetComponentLookup<Game.Net.CarLane>(true);
        m_CurveData = GetComponentLookup<Curve>(true);
        m_DistrictData = GetComponentLookup<District>(true);
        m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
        m_Edges = GetBufferLookup<ConnectedEdge>(true);
        m_HiddenData = GetComponentLookup<Hidden>(true);
        m_LaneData = GetComponentLookup<Lane>(true);
        m_MasterLaneData = GetComponentLookup<MasterLane>(true);
        m_NetCompositionData = GetComponentLookup<Composition>(true);
        m_NetEdgeData = GetComponentLookup<Game.Net.Edge>(true);
        m_PedestrianLaneData = GetComponentLookup<Game.Net.PedestrianLane>(true);
        m_PrefabLaneDirectionData = GetComponentLookup<LaneDirectionData>(true);
        m_PrefabNetCompositionData = GetComponentLookup<NetCompositionData>(true);
        m_PrefabTrafficSignData = GetComponentLookup<Game.Prefabs.TrafficSignData>(true);
        m_SecondaryLaneData = GetComponentLookup<Game.Net.SecondaryLane>(true);
        m_SlaveLaneData = GetComponentLookup<SlaveLane>(true);
        m_SubLanes = GetBufferLookup<Game.Net.SubLane>(true);
        m_TempData = GetComponentLookup<Temp>(true);
        _owner = owner; m_LeftHandTraffic = leftHandTraffic;
        _context = new LaneContext(); _collected = false;
        var data = m_PrefabNetCompositionData[m_NetCompositionData[owner].m_Edge];
        _forbidBicycles = new bool2((data.m_Flags.m_Left & CompositionFlags.Side.ForbidSecondary) != 0,
            (data.m_Flags.m_Right & CompositionFlags.Side.ForbidSecondary) != 0);
        if ((data.m_Flags.m_General & CompositionFlags.General.Invert) != 0)
            _forbidBicycles = _forbidBicycles.yx;
        if (!math.all(_forbidBicycles) && m_BorderDistrictData.TryGetComponent(owner, out var district))
            _forbidBicycles |= AreaUtils.CheckOption(district, DistrictOption.ForbidBicycles, ref m_DistrictData);
        _neighborForbidBicycles = _forbidBicycles;
    }

    internal TrafficSignNeeds Calculate(Entity objectPrefab, float3 position)
        => Calculate(m_PrefabTrafficSignData.HasComponent(objectPrefab),
            m_PrefabLaneDirectionData.HasComponent(objectPrefab), position);

    // The lane world may use remapped prefab identities. The source-prefab world can provide
    // these two native component-presence flags without leaking its entities into lane lookups.
    internal TrafficSignNeeds Calculate(bool isTrafficSign, bool isLaneDirection, float3 position)
    {
        var owner = _owner;
        var flag2 = isTrafficSign;
        var flag3 = isLaneDirection;
TrafficSignNeeds trafficSignNeeds = new TrafficSignNeeds
					{
						m_Left = LaneDirectionType.None,
						m_Forward = LaneDirectionType.None,
						m_Right = LaneDirectionType.None
					};
					if ((flag2 || flag3) && GetClosestCarLane(owner, position, 1f, out var result, out var hasTurningLanes, out var bicycleOnly, out var pedestrianOnly))
					{
						if (pedestrianOnly)
						{
							if (flag2)
							{
								trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.PedestrianOnly);
							}
						}
						else
						{
							Game.Net.CarLane carLane = m_CarLaneData[result];
							Lane lane = m_LaneData[result];
							if (m_SlaveLaneData.TryGetComponent(result, out var componentData4))
							{
								DynamicBuffer<Game.Net.SubLane> dynamicBuffer2 = m_SubLanes[owner];
								if (componentData4.m_MasterIndex < dynamicBuffer2.Length)
								{
									lane = m_LaneData[dynamicBuffer2[componentData4.m_MasterIndex].m_SubLane];
								}
								if (flag3)
								{
									if ((componentData4.m_Flags & SlaveLaneFlags.MergeLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.Merge;
									}
									if ((componentData4.m_Flags & SlaveLaneFlags.MergeRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.Merge;
									}
								}
							}
							if (!_collected)
							{
								_collected = true;
								Game.Net.Edge edge = m_NetEdgeData[owner];
								AddNodeLanes(edge.m_Start, owner, ref _neighborForbidBicycles.x, ref _context);
								AddNodeLanes(edge.m_End, owner, ref _neighborForbidBicycles.y, ref _context);
								AddEdgeLanes(owner, ref _context);
							}
							if (flag3)
							{
								bool flag4 = false;
								if (!hasTurningLanes && _context.m_TargetLanes.TryGetValue(lane.m_EndNode, out var item))
								{
									flag4 = (item.m_CarLaneFlags & CarLaneFlags.Forbidden) != 0 && (item.m_CarLaneFlags & (CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != 0;
								}
								if (hasTurningLanes || flag4)
								{
									if ((carLane.m_Flags & CarLaneFlags.UTurnLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.UTurn;
									}
									if ((carLane.m_Flags & CarLaneFlags.UTurnRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.UTurn;
									}
									if ((carLane.m_Flags & CarLaneFlags.GentleTurnLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.Gentle;
									}
									if ((carLane.m_Flags & CarLaneFlags.GentleTurnRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.Gentle;
									}
									if ((carLane.m_Flags & CarLaneFlags.TurnLeft) != 0)
									{
										trafficSignNeeds.m_Left = LaneDirectionType.Square;
									}
									if ((carLane.m_Flags & CarLaneFlags.TurnRight) != 0)
									{
										trafficSignNeeds.m_Right = LaneDirectionType.Square;
									}
									if ((carLane.m_Flags & CarLaneFlags.Forward) != 0)
									{
										trafficSignNeeds.m_Forward = LaneDirectionType.Straight;
									}
								}
							}
							if (flag2)
							{
								TargetLaneData item2 = default(TargetLaneData);
								EdgeLane componentData6;
								if (_context.m_SourceLanes.TryGetValue(lane.m_StartNode, out item2))
								{
									if ((carLane.m_Flags & ~item2.m_AndCarLaneFlags & CarLaneFlags.PublicOnly) != 0 && (item2.m_CarLaneFlags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.PublicOnly | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != CarLaneFlags.PublicOnly)
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.BusOnly);
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.TaxiOnly);
									}
									else if (math.any(math.abs(item2.m_SpeedLimit - carLane.m_DefaultSpeedLimit) > 1f))
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.SpeedLimit);
										trafficSignNeeds.m_SpeedLimit = (ushort)Mathf.RoundToInt(carLane.m_DefaultSpeedLimit * 3.6f);
									}
									if (!bicycleOnly && math.any(_forbidBicycles) && (item2.m_AndCarLaneFlags & CarLaneFlags.ForbidBicycles) == 0 && m_EdgeLaneData.TryGetComponent(result, out var componentData5) && ((componentData5.m_EdgeDelta.x < componentData5.m_EdgeDelta.y) ? _forbidBicycles.x : _forbidBicycles.y))
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoBicycles);
									}
								}
								else if (!bicycleOnly && math.any(_forbidBicycles) && m_EdgeLaneData.TryGetComponent(result, out componentData6) && ((componentData6.m_EdgeDelta.x < componentData6.m_EdgeDelta.y) ? (_forbidBicycles.x & !_neighborForbidBicycles.x) : (_forbidBicycles.y & !_neighborForbidBicycles.y)))
								{
									trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.NoBicycles);
								}
								if ((carLane.m_Flags & CarLaneFlags.Roundabout) != 0)
								{
									if (m_LeftHandTraffic)
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.RoundaboutClockwise);
									}
									else
									{
										trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.RoundaboutCounterclockwise);
									}
								}
								if (bicycleOnly)
								{
									trafficSignNeeds.m_SignTypeMask |= Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.BicycleOnly);
								}
							}
						}
					}
					
        return trafficSignNeeds;
    }
		private void AddNodeLanes(Entity node, Entity edge, ref bool forbidBicycles, ref LaneContext updateData)
		{
			if (!m_SubLanes.TryGetBuffer(node, out var bufferData))
			{
				return;
			}
			if (forbidBicycles)
			{
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_Edges, m_NetEdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					if (!(value.m_Edge == edge))
					{
						Composition composition = m_NetCompositionData[value.m_Edge];
						NetCompositionData netCompositionData = m_PrefabNetCompositionData[composition.m_Edge];
						bool flag = (((value.m_End == ((netCompositionData.m_Flags.m_General & CompositionFlags.General.Invert) != 0)) ? netCompositionData.m_Flags.m_Left : netCompositionData.m_Flags.m_Right) & CompositionFlags.Side.ForbidSecondary) != 0;
						if (!flag && m_BorderDistrictData.TryGetComponent(value.m_Edge, out var componentData))
						{
							flag |= AreaUtils.CheckOption(componentData, DistrictOption.ForbidBicycles, ref m_DistrictData);
						}
						forbidBicycles &= flag;
					}
				}
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Net.SubLane subLane = bufferData[i];
				if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) == 0 || m_SecondaryLaneData.HasComponent(subLane.m_SubLane) || m_MasterLaneData.HasComponent(subLane.m_SubLane) || !m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData2))
				{
					continue;
				}
				if ((componentData2.m_Flags & CarLaneFlags.Unsafe) == 0)
				{
					Lane lane = m_LaneData[subLane.m_SubLane];
					if (m_SlaveLaneData.TryGetComponent(subLane.m_SubLane, out var componentData3) && componentData3.m_MasterIndex < bufferData.Length)
					{
						lane = m_LaneData[bufferData[componentData3.m_MasterIndex].m_SubLane];
					}
					if (forbidBicycles || (subLane.m_PathMethods & PathMethod.Bicycle) == 0)
					{
						componentData2.m_Flags |= CarLaneFlags.ForbidBicycles;
					}
					if (updateData.m_SourceLanes.TryGetValue(lane.m_EndNode, out var item))
					{
						item.m_CarLaneFlags |= componentData2.m_Flags;
						item.m_AndCarLaneFlags &= componentData2.m_Flags;
						item.m_SpeedLimit.x = math.min(item.m_SpeedLimit.x, componentData2.m_DefaultSpeedLimit);
						item.m_SpeedLimit.y = math.max(item.m_SpeedLimit.y, componentData2.m_DefaultSpeedLimit);
						updateData.m_SourceLanes[lane.m_EndNode] = item;
					}
					else
					{
						item = new TargetLaneData
						{
							m_CarLaneFlags = componentData2.m_Flags,
							m_AndCarLaneFlags = componentData2.m_Flags,
							m_SpeedLimit = componentData2.m_DefaultSpeedLimit
						};
						updateData.m_SourceLanes.Add(lane.m_EndNode, item);
					}
				}
				else if ((componentData2.m_Flags & CarLaneFlags.Forbidden) != 0)
				{
					Lane lane2 = m_LaneData[subLane.m_SubLane];
					if (m_SlaveLaneData.TryGetComponent(subLane.m_SubLane, out var componentData4) && componentData4.m_MasterIndex < bufferData.Length)
					{
						lane2 = m_LaneData[bufferData[componentData4.m_MasterIndex].m_SubLane];
					}
					if (updateData.m_TargetLanes.TryGetValue(lane2.m_StartNode, out var item2))
					{
						item2.m_CarLaneFlags |= componentData2.m_Flags;
						item2.m_AndCarLaneFlags &= componentData2.m_Flags;
						item2.m_SpeedLimit.x = math.min(item2.m_SpeedLimit.x, componentData2.m_DefaultSpeedLimit);
						item2.m_SpeedLimit.y = math.max(item2.m_SpeedLimit.y, componentData2.m_DefaultSpeedLimit);
						updateData.m_TargetLanes[lane2.m_StartNode] = item2;
					}
					else
					{
						item2 = new TargetLaneData
						{
							m_CarLaneFlags = componentData2.m_Flags,
							m_AndCarLaneFlags = componentData2.m_Flags,
							m_SpeedLimit = componentData2.m_DefaultSpeedLimit
						};
						updateData.m_TargetLanes.Add(lane2.m_StartNode, item2);
					}
				}
			}
		}

		private void AddEdgeLanes(Entity edge, ref LaneContext updateData)
		{
			if (!m_SubLanes.TryGetBuffer(edge, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Net.SubLane subLane = bufferData[i];
				if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) != 0 && !m_SecondaryLaneData.HasComponent(subLane.m_SubLane) && !m_MasterLaneData.HasComponent(subLane.m_SubLane) && m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData) && (componentData.m_Flags & CarLaneFlags.Unsafe) == 0)
				{
					Lane lane = m_LaneData[subLane.m_SubLane];
					if (m_SlaveLaneData.TryGetComponent(subLane.m_SubLane, out var componentData2) && componentData2.m_MasterIndex < bufferData.Length)
					{
						lane = m_LaneData[bufferData[componentData2.m_MasterIndex].m_SubLane];
					}
					if (updateData.m_SourceLanes.TryGetValue(lane.m_StartNode, out var item) && (item.m_CarLaneFlags & CarLaneFlags.Approach) == 0)
					{
						item.m_CarLaneFlags |= CarLaneFlags.Approach;
						updateData.m_SourceLanes.TryAdd(lane.m_EndNode, item);
					}
					if (updateData.m_TargetLanes.TryGetValue(lane.m_EndNode, out var item2) && (item2.m_CarLaneFlags & CarLaneFlags.Approach) == 0)
					{
						item2.m_CarLaneFlags |= CarLaneFlags.Approach;
						updateData.m_TargetLanes.TryAdd(lane.m_StartNode, item2);
					}
				}
			}
		}

		private bool GetClosestCarLane(Entity owner, float3 position, float maxDistance, out Entity result, out bool hasTurningLanes, out bool bicycleOnly, out bool pedestrianOnly)
		{
			result = Entity.Null;
			hasTurningLanes = false;
			bicycleOnly = false;
			pedestrianOnly = false;
			if (!m_SubLanes.TryGetBuffer(owner, out var bufferData))
			{
				return false;
			}
			bool2 @bool = false;
			bool flag = false;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Game.Net.SubLane subLane = bufferData[i];
				bool flag2 = false;
				Game.Net.PedestrianLane componentData2;
				if ((subLane.m_PathMethods & (PathMethod.Road | PathMethod.Bicycle)) != 0)
				{
					if (!m_CarLaneData.TryGetComponent(subLane.m_SubLane, out var componentData) || m_MasterLaneData.HasComponent(subLane.m_SubLane) || m_SecondaryLaneData.HasComponent(subLane.m_SubLane) || (componentData.m_Flags & CarLaneFlags.Unsafe) != 0)
					{
						continue;
					}
					if ((componentData.m_Flags & CarLaneFlags.Invert) != 0)
					{
						@bool.y |= (componentData.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != 0;
					}
					else
					{
						@bool.x |= (componentData.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.TurnLeft | CarLaneFlags.TurnRight | CarLaneFlags.UTurnRight | CarLaneFlags.GentleTurnLeft | CarLaneFlags.GentleTurnRight)) != 0;
					}
					flag2 = (componentData.m_Flags & CarLaneFlags.Invert) != 0;
				}
				else if ((subLane.m_PathMethods & PathMethod.Pedestrian) == 0 || !m_PedestrianLaneData.TryGetComponent(subLane.m_SubLane, out componentData2) || (componentData2.m_Flags & PedestrianLaneFlags.Unsafe) != 0)
				{
					continue;
				}
				float t;
				float num = MathUtils.Distance(m_CurveData[subLane.m_SubLane].m_Bezier, position, out t);
				if (num < maxDistance)
				{
					maxDistance = num;
					result = subLane.m_SubLane;
					flag = flag2;
					bicycleOnly = subLane.m_PathMethods == PathMethod.Bicycle;
					pedestrianOnly = subLane.m_PathMethods == PathMethod.Pedestrian;
				}
			}
			hasTurningLanes = (flag ? @bool.y : @bool.x);
			return result != Entity.Null;
		}

}
