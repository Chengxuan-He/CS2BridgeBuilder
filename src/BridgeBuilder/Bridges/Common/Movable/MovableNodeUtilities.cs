using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace BridgeBuilder.Bridges;

// Utility-lane node object arbitration from native 1.6.2f1 SecondaryObjectSystem.
// All entity references remain in the prepared lane world, never in saved assets.
[DisableAutoCreation]
internal sealed partial class MovableNodeUtilities : SystemBase
{
    internal struct UtilityNodeData
    {
        internal Transform m_Transform;
        internal Entity m_Prefab;
        internal int m_Count;
        internal float m_LanePriority, m_NodePriority, m_Elevation;
        internal UtilityTypes m_UtilityTypes;
        internal bool m_Unsure, m_Vertical;
    }
    private readonly List<UtilityNodeData> _nodes = new();
    protected override void OnUpdate() { }
    private ComponentLookup<PrefabRef> m_PrefabRefData;
    private ComponentLookup<UtilityLaneData> m_PrefabUtilityLaneData;
    private ComponentLookup<Curve> m_CurveData;
    private ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;
    private ComponentLookup<EdgeLane> m_EdgeLaneData;
    private ComponentLookup<Game.Net.UtilityLane> m_UtilityLaneData;
    private ComponentLookup<Game.Net.Elevation> m_NetElevationData;
    private ComponentLookup<Game.Net.Node> m_NetNodeData;
    private ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;
    private BufferLookup<DefaultNetLane> m_DefaultNetLanes;

    internal void Begin()
    {
        _nodes.Clear();
        m_PrefabRefData = GetComponentLookup<PrefabRef>(true);
        m_PrefabUtilityLaneData = GetComponentLookup<UtilityLaneData>(true);
        m_CurveData = GetComponentLookup<Curve>(true);
        m_PrefabObjectGeometryData = GetComponentLookup<ObjectGeometryData>(true);
        m_EdgeLaneData = GetComponentLookup<EdgeLane>(true);
        m_UtilityLaneData = GetComponentLookup<Game.Net.UtilityLane>(true);
        m_NetElevationData = GetComponentLookup<Game.Net.Elevation>(true);
        m_NetNodeData = GetComponentLookup<Game.Net.Node>(true);
        m_PrefabNetGeometryData = GetComponentLookup<NetGeometryData>(true);
        m_DefaultNetLanes = GetBufferLookup<DefaultNetLane>(true);
    }

    internal void CollectEdge(Entity edge, bool isEnd, bool isMiddle)
    {
        var lanes = EntityManager.GetBuffer<Game.Net.SubLane>(edge, true);
        for (var i = 0; i < lanes.Length; i++)
        {
            var subLane = lanes[i].m_SubLane;
            if (EntityManager.HasComponent<Game.Net.SecondaryLane>(subLane) ||
                !m_UtilityLaneData.HasComponent(subLane)) continue;

						PrefabRef prefabRef2 = m_PrefabRefData[subLane];
						UtilityLaneData utilityLaneData = m_PrefabUtilityLaneData[prefabRef2.m_Prefab];
						if (!(utilityLaneData.m_NodeObjectPrefab != Entity.Null))
						{
							continue;
						}
						Curve curve4 = m_CurveData[subLane];
						float num19 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData.m_NodeObjectPrefab].m_Bounds));
						bool2 x4 = false;
						bool flag = false;
						if (m_EdgeLaneData.HasComponent(subLane))
						{
							if (!isMiddle)
							{
								EdgeLane edgeLane = m_EdgeLaneData[subLane];
								x4 = (!isEnd & (edgeLane.m_EdgeDelta == 0f)) | (isEnd & (edgeLane.m_EdgeDelta == 1f));
							}
						}
						else if (isMiddle)
						{
							x4 = new bool2(x: false, y: true);
							flag = true;
						}
						if (!math.any(x4))
						{
							continue;
						}
						
						for (int k = 0; k < _nodes.Count; k++)
						{
							UtilityNodeData value5 = _nodes[k];
							if ((value5.m_UtilityTypes & utilityLaneData.m_UtilityTypes) == 0)
							{
								continue;
							}
							if (x4.x && math.distancesq(value5.m_Transform.m_Position, curve4.m_Bezier.a) < 0.01f)
							{
								value5.m_Unsure &= flag;
								if (!flag && num19 > value5.m_NodePriority)
								{
									value5.m_Prefab = utilityLaneData.m_NodeObjectPrefab;
									value5.m_NodePriority = num19;
								}
								if (num19 > value5.m_LanePriority)
								{
									value5.m_LanePriority = num19;
									value5.m_Count = 1;
									value5.m_Vertical = flag;
								}
								else if (num19 == value5.m_LanePriority)
								{
									value5.m_Count++;
									value5.m_Vertical |= flag;
								}
								_nodes[k] = value5;
								x4.x = false;
								if (!x4.y)
								{
									break;
								}
							}
							if (x4.y && math.distancesq(value5.m_Transform.m_Position, curve4.m_Bezier.d) < 0.01f)
							{
								value5.m_Unsure &= flag;
								if (!flag && num19 > value5.m_NodePriority)
								{
									value5.m_Prefab = utilityLaneData.m_NodeObjectPrefab;
									value5.m_NodePriority = num19;
								}
								if (num19 > value5.m_LanePriority)
								{
									value5.m_LanePriority = num19;
									value5.m_Count = 1;
									value5.m_Vertical = flag;
								}
								else if (num19 == value5.m_LanePriority)
								{
									value5.m_Count++;
									value5.m_Vertical |= flag;
								}
								_nodes[k] = value5;
								x4.y = false;
								if (!x4.x)
								{
									break;
								}
							}
						}
						if (x4.x)
						{
							UtilityNodeData value6 = new UtilityNodeData
							{
								m_Transform = new Transform(curve4.m_Bezier.a, NetUtils.GetNodeRotation(MathUtils.StartTangent(curve4.m_Bezier))),
								m_Prefab = utilityLaneData.m_NodeObjectPrefab,
								m_Count = 1,
								m_LanePriority = num19,
								m_NodePriority = math.select(num19, 0f, flag),
								m_UtilityTypes = utilityLaneData.m_UtilityTypes,
								m_Unsure = flag,
								m_Vertical = flag
							};
							_nodes.Add(value6);
						}
						if (x4.y)
						{
							UtilityNodeData value7 = new UtilityNodeData
							{
								m_Transform = new Transform(curve4.m_Bezier.d, NetUtils.GetNodeRotation(MathUtils.EndTangent(curve4.m_Bezier))),
								m_Prefab = utilityLaneData.m_NodeObjectPrefab,
								m_Count = 1,
								m_LanePriority = num19,
								m_NodePriority = math.select(num19, 0f, flag),
								m_UtilityTypes = utilityLaneData.m_UtilityTypes,
								m_Unsure = flag,
								m_Vertical = flag
							};
							_nodes.Add(value7);
						}
					
        }
    }

    // Native isolated-node fallback, used only when there were no non-middle contributions.
    internal void CollectIsolatedNode(Entity owner)
    {

				PrefabRef prefabRef3 = m_PrefabRefData[owner];
				if (m_DefaultNetLanes.TryGetBuffer(prefabRef3.m_Prefab, out var bufferData))
				{
					Game.Net.Node node = m_NetNodeData[owner];
					NetGeometryData netGeometryData2 = m_PrefabNetGeometryData[prefabRef3.m_Prefab];
					for (int num35 = 0; num35 < bufferData.Length; num35++)
					{
						NetCompositionLane netCompositionLane = new NetCompositionLane(bufferData[num35]);
						if ((netCompositionLane.m_Flags & LaneFlags.Utility) == 0 || (netCompositionLane.m_Flags & LaneFlags.FindAnchor) != 0)
						{
							continue;
						}
						bool flag3 = (netCompositionLane.m_Flags & LaneFlags.Invert) != 0;
						if (((uint)netCompositionLane.m_Flags & (uint)(flag3 ? 512 : 256)) == 0)
						{
							UtilityLaneData utilityLaneData2 = m_PrefabUtilityLaneData[netCompositionLane.m_Lane];
							if (utilityLaneData2.m_NodeObjectPrefab != Entity.Null)
							{
								float num36 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData2.m_NodeObjectPrefab].m_Bounds));
								netCompositionLane.m_Position.x = 0f - netCompositionLane.m_Position.x;
								float t5 = netCompositionLane.m_Position.x / math.max(1f, netGeometryData2.m_DefaultWidth) + 0.5f;
								float3 end2 = node.m_Position + math.rotate(node.m_Rotation, new float3(netGeometryData2.m_DefaultWidth * -0.5f, 0f, 0f));
								float3 position2 = math.lerp(node.m_Position + math.rotate(node.m_Rotation, new float3(netGeometryData2.m_DefaultWidth * 0.5f, 0f, 0f)), end2, t5);
								UtilityNodeData value11 = new UtilityNodeData
								{
									m_Transform = 
									{
										m_Position = position2
									}
								};
								value11.m_Transform.m_Position.y += netCompositionLane.m_Position.y;
								value11.m_Transform.m_Rotation = quaternion.identity;
								value11.m_Prefab = utilityLaneData2.m_NodeObjectPrefab;
								value11.m_Count = 1;
								value11.m_Elevation = netCompositionLane.m_Position.y;
								value11.m_LanePriority = num36;
								value11.m_NodePriority = num36;
								value11.m_UtilityTypes = utilityLaneData2.m_UtilityTypes;
								
								_nodes.Add(value11);
							}
						}
					}
				}
			
    }

    internal void CollectNode(Entity owner)
    {
        var lanes = EntityManager.GetBuffer<Game.Net.SubLane>(owner, true);
        for (var i = 0; i < lanes.Length; i++)
        {
            var subLane2 = lanes[i].m_SubLane;
            if (EntityManager.HasComponent<Game.Net.SecondaryLane>(subLane2) ||
                !m_UtilityLaneData.HasComponent(subLane2)) continue;

					PrefabRef prefabRef4 = m_PrefabRefData[subLane2];
					UtilityLaneData utilityLaneData3 = m_PrefabUtilityLaneData[prefabRef4.m_Prefab];
					if (!(utilityLaneData3.m_NodeObjectPrefab != Entity.Null))
					{
						continue;
					}
					Curve curve6 = m_CurveData[subLane2];
					if (curve6.m_Length <= 0.1f)
					{
						continue;
					}
					Game.Net.UtilityLane utilityLane = m_UtilityLaneData[subLane2];
					float num38 = math.length(MathUtils.Size(m_PrefabObjectGeometryData[utilityLaneData3.m_NodeObjectPrefab].m_Bounds));
					bool2 @bool = false;
					bool flag4 = (utilityLane.m_Flags & UtilityLaneFlags.VerticalConnection) != 0;
					
					for (int num39 = 0; num39 < _nodes.Count; num39++)
					{
						UtilityNodeData value12 = _nodes[num39];
						if ((value12.m_UtilityTypes & utilityLaneData3.m_UtilityTypes) == 0)
						{
							continue;
						}
						if (!@bool.x && math.distancesq(value12.m_Transform.m_Position, curve6.m_Bezier.a) < 0.01f)
						{
							value12.m_Unsure = false;
							if (!flag4 && num38 > value12.m_NodePriority)
							{
								value12.m_Prefab = utilityLaneData3.m_NodeObjectPrefab;
								value12.m_NodePriority = num38;
							}
							if (num38 > value12.m_LanePriority)
							{
								value12.m_LanePriority = num38;
								value12.m_Count = 1;
								value12.m_Vertical = flag4;
							}
							else if (num38 == value12.m_LanePriority)
							{
								value12.m_Count++;
								value12.m_Vertical |= flag4;
							}
							_nodes[num39] = value12;
							@bool.x = true;
							if (@bool.y)
							{
								break;
							}
						}
						if (!@bool.y && math.distancesq(value12.m_Transform.m_Position, curve6.m_Bezier.d) < 0.01f)
						{
							value12.m_Unsure = false;
							if (!flag4 && num38 > value12.m_NodePriority)
							{
								value12.m_Prefab = utilityLaneData3.m_NodeObjectPrefab;
								value12.m_NodePriority = num38;
							}
							if (num38 > value12.m_LanePriority)
							{
								value12.m_LanePriority = num38;
								value12.m_Count = 1;
								value12.m_Vertical = flag4;
							}
							else if (num38 == value12.m_LanePriority)
							{
								value12.m_Count++;
								value12.m_Vertical |= flag4;
							}
							_nodes[num39] = value12;
							@bool.y = true;
							if (@bool.x)
							{
								break;
							}
						}
					}
					if (!@bool.x)
					{
						float elevation = 0f;
						if (m_NetElevationData.TryGetComponent(subLane2, out var componentData3) && componentData3.m_Elevation.x != float.MinValue)
						{
							elevation = componentData3.m_Elevation.x;
						}
						UtilityNodeData value13 = new UtilityNodeData
						{
							m_Transform = new Transform(curve6.m_Bezier.a, NetUtils.GetNodeRotation(MathUtils.StartTangent(curve6.m_Bezier))),
							m_Prefab = utilityLaneData3.m_NodeObjectPrefab,
							m_Count = 1,
							m_Elevation = elevation,
							m_LanePriority = num38,
							m_NodePriority = num38,
							m_UtilityTypes = utilityLaneData3.m_UtilityTypes,
							m_Vertical = flag4
						};
						_nodes.Add(value13);
					}
					if (!@bool.y)
					{
						float elevation2 = 0f;
						if (m_NetElevationData.TryGetComponent(subLane2, out var componentData4) && componentData4.m_Elevation.y != float.MinValue)
						{
							elevation2 = componentData4.m_Elevation.y;
						}
						UtilityNodeData value14 = new UtilityNodeData
						{
							m_Transform = new Transform(curve6.m_Bezier.d, NetUtils.GetNodeRotation(MathUtils.EndTangent(curve6.m_Bezier))),
							m_Prefab = utilityLaneData3.m_NodeObjectPrefab,
							m_Count = 1,
							m_Elevation = elevation2,
							m_LanePriority = num38,
							m_NodePriority = num38,
							m_UtilityTypes = utilityLaneData3.m_UtilityTypes,
							m_Vertical = flag4
						};
						_nodes.Add(value14);
					}
				
        }
    }

    // Native final emission predicate. A simple two-way joint has no extra node object;
    // a vertical connection or other degree does. Preserve collected order for selection.
    internal IEnumerable<UtilityNodeData> Placements()
    {
        foreach (var node in _nodes)
            if (!node.m_Unsure && (node.m_Count != 2 || node.m_Vertical)) yield return node;
    }
}
