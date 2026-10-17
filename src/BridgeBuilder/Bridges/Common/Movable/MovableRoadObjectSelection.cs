// Selection logic derived from game 1.6.2f1 SecondaryObjectSystem.CreateSecondaryObject.
// Native input is read-only. This class selects a concrete prefab; it creates no ECS entities.
using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Objects;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace BridgeBuilder.Bridges;

internal sealed class MovableRoadObjectSelection
{
    private readonly EntityManager _manager;
    private readonly PrefabSystem _prefabs;
    private readonly Entity _theme;
    private readonly Dictionary<(Entity, int), Random> _groups = new();

    internal MovableRoadObjectSelection(EntityManager manager, PrefabSystem prefabs, Entity defaultTheme)
    { _manager = manager; _prefabs = prefabs; _theme = defaultTheme; }

    // Complete the placement/selection callback with the native anchor and rotation rules.
    // Returned placements are still in the closed-road frame; MovableRoadObjects.Bind moves
    // them to the mechanism frame and assigns the actual parent connection.
    internal bool TryResolve(MovableRoadObjectPlacement.Candidate candidate, TrafficSignNeeds needs,
        float ownerElevation, ref Random random, out ObjectSubObjectInfo? placement)
    {
        placement = null;
        if (MovableSectionObjects.IsNetworkSignal(candidate.Prefab))
            throw new InvalidOperationException("Network signal rules must be resolved by native network ownership, not baked as mechanism children: " + candidate.Prefab.name);
        if (!_prefabs.TryGetEntity(candidate.Prefab, out var source))
            throw new InvalidOperationException($"Road object '{candidate.Prefab.name}' has no native selection data.");
        if (!TrySelect(source, needs, ref random, 0, candidate.Probability, out var selected)) return false;
        if (!_prefabs.TryGetPrefab<ObjectPrefab>(selected.Prefab, out var prefab) || prefab == null)
            throw new InvalidOperationException("Native road object selection returned an unresolved Prefab.");
        var transform = candidate.Object;
        var flags = candidate.Flags;
        if (selected.Flipped)
            transform.m_Rotation = math.mul(quaternion.RotateY(MathF.PI), transform.m_Rotation);
        var placeable = TryRead<PlaceableObjectData>(selected.Prefab, out var placeableData);
        if ((flags & SubObjectFlags.AnchorTop) != 0)
        {
            var geometry = _manager.GetComponentData<ObjectGeometryData>(selected.Prefab);
            transform.m_Position.y -= geometry.m_Bounds.max.y - placeableData.m_PlacementOffset.y;
        }
        else if ((flags & SubObjectFlags.AnchorCenter) != 0)
        {
            var geometry = _manager.GetComponentData<ObjectGeometryData>(selected.Prefab);
            transform.m_Position.y -= (geometry.m_Bounds.max.y - geometry.m_Bounds.min.y) * .5f;
        }
        else if ((flags & SubObjectFlags.OnGround) != 0)
        {
            transform.m_Position.y = candidate.Parent.m_Position.y - ownerElevation;
        }
        var objectRandom = selected.Random;
        var variation = objectRandom.NextUInt(268435456u);
        if (placeable && placeableData.m_RotationSymmetry != RotationSymmetry.None)
        {
            var orientation = variation & 0xFFF;
            if (placeableData.m_RotationSymmetry != RotationSymmetry.Any)
                orientation = (uint)(((int)orientation * (int)placeableData.m_RotationSymmetry) & -4096) /
                    (uint)placeableData.m_RotationSymmetry;
            transform.m_Rotation = math.mul(quaternion.RotateY(orientation * .0015339808f), transform.m_Rotation);
        }
        placement = new ObjectSubObjectInfo
        {
            m_Object = prefab, m_Position = transform.m_Position, m_Rotation = transform.m_Rotation,
            m_ParentMesh = 0, m_GroupIndex = selected.GroupIndex,
            // Selection already applied the native placement probability. Do not roll it again.
            m_Probability = 100
        };
        return true;
    }

    internal readonly struct Selection
    {
        internal readonly Entity Prefab;
        internal readonly int GroupIndex;
        internal readonly Random Random;
        internal readonly bool Flipped;
        internal Selection(Entity prefab, int groupIndex, Random random, bool flipped)
        { Prefab = prefab; GroupIndex = groupIndex; Random = random; Flipped = flipped; }
    }

	internal struct TrafficSignNeeds
	{
		public uint m_SignTypeMask;

		public uint m_RemoveSignTypeMask;

		public uint m_SignTypeMask2;

		public uint m_RemoveSignTypeMask2;

		public ushort m_SpeedLimit;

		public ushort m_SpeedLimit2;

		public float m_VehicleLanesLeft;

		public float m_VehicleLanesRight;

		public ushort m_VehicleMask;

		public ushort m_CrossingLeftMask;

		public ushort m_CrossingRightMask;

		public LaneDirectionType m_Left;

		public LaneDirectionType m_Forward;

		public LaneDirectionType m_Right;
	}

    // Share one selector per closed-road object pass so native randomization groups stay coherent.
    // Traffic needs must come from generated native lanes, not from the displayed lane count.
    internal bool TrySelect(Entity prefab, TrafficSignNeeds trafficSignNeeds,
        ref Random random, int groupIndex, int probability, out Selection selection)
    {
        selection = default;

			if (!TryRead<PlaceholderObjectData>(prefab, out var componentData) || !TryBuffer<PlaceholderObjectElement>(prefab, out var bufferData))
			{
				Entity groupPrefab = prefab;
				if (TryRead<SpawnableObjectData>(prefab, out var componentData2) && componentData2.m_RandomizationGroup != Entity.Null)
				{
					groupPrefab = componentData2.m_RandomizationGroup;
				}
				if (CheckRequirements(prefab, true))
				{
					Unity.Mathematics.Random random2 = random;
					random.NextInt();
					random.NextInt();
					if (_groups.TryGetValue((groupPrefab, groupIndex), out var item))
					{
						random2 = item;
					}
					else
					{
						_groups.TryAdd((groupPrefab, groupIndex), random2);
					}
					if (random2.NextInt(100) < probability)
					{
						selection = new Selection(prefab, groupIndex, random2, false);
return true;
					}
				}
				return false;
			}
			if (componentData.m_RandomizeGroupIndex)
			{
				groupIndex = random.NextInt();
			}
			float num = -1f;
			Entity prefab2 = Entity.Null;
			Entity groupPrefab2 = Entity.Null;
			Unity.Mathematics.Random random3 = default(Unity.Mathematics.Random);
			bool flag = false;
			int num2 = 0;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity entity = bufferData[i].m_Object;
				if (!CheckRequirements(entity, false))
				{
					continue;
				}
				float num3 = 0f;
				bool flag2 = false;
				if (_manager.HasComponent<TrafficSignData>(entity))
				{
					Game.Prefabs.TrafficSignData trafficSignData = _manager.GetComponentData<TrafficSignData>(entity);
					uint num4 = trafficSignData.m_TypeMask & trafficSignNeeds.m_SignTypeMask;
					int num5 = trafficSignNeeds.m_SpeedLimit;
					if (num4 == 0)
					{
						num4 = trafficSignData.m_TypeMask & trafficSignNeeds.m_SignTypeMask2;
						num5 = trafficSignNeeds.m_SpeedLimit2;
						if (num4 == 0)
						{
							continue;
						}
						flag2 = true;
					}
					float num6 = 10f + math.log2(num4);
					if ((num4 & Game.Prefabs.TrafficSignData.GetTypeMask(TrafficSignType.SpeedLimit)) != 0)
					{
						num6 /= 1f + (float)math.abs(trafficSignData.m_SpeedLimit - num5);
					}
					num3 += num6;
				}
				if (_manager.HasComponent<TrafficLightData>(entity))
				{
					TrafficLightData trafficLightData = _manager.GetComponentData<TrafficLightData>(entity);
					int num7 = 0;
					if (trafficSignNeeds.m_VehicleLanesLeft > 0f)
					{
						if ((trafficLightData.m_Type & TrafficLightType.VehicleLeft) == 0)
						{
							continue;
						}
						num7 += 10;
					}
					else if ((trafficLightData.m_Type & TrafficLightType.VehicleLeft) != 0)
					{
						if (trafficSignNeeds.m_VehicleMask == 0)
						{
							continue;
						}
						num7--;
					}
					if (trafficSignNeeds.m_VehicleLanesRight > 0f)
					{
						if ((trafficLightData.m_Type & TrafficLightType.VehicleRight) == 0)
						{
							continue;
						}
						num7 += 10;
					}
					else if ((trafficLightData.m_Type & TrafficLightType.VehicleRight) != 0)
					{
						if (trafficSignNeeds.m_VehicleMask == 0)
						{
							continue;
						}
						num7--;
					}
					if (trafficSignNeeds.m_CrossingLeftMask != 0 && trafficSignNeeds.m_CrossingRightMask != 0)
					{
						num7 = (((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) != (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) ? (num7 - 1) : (num7 + 10));
					}
					else if (trafficSignNeeds.m_CrossingLeftMask != 0)
					{
						if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) == TrafficLightType.CrossingLeft)
						{
							num7 += 10;
						}
						else if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight | TrafficLightType.AllowFlipped)) == (TrafficLightType.CrossingRight | TrafficLightType.AllowFlipped))
						{
							flag2 = true;
							num7 += 9;
						}
						else
						{
							if ((trafficLightData.m_Type & TrafficLightType.CrossingRight) != 0)
							{
								continue;
							}
							num7--;
						}
					}
					else if (trafficSignNeeds.m_CrossingRightMask != 0)
					{
						if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) == TrafficLightType.CrossingRight)
						{
							num7 += 10;
						}
						else if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight | TrafficLightType.AllowFlipped)) == (TrafficLightType.CrossingLeft | TrafficLightType.AllowFlipped))
						{
							flag2 = true;
							num7 += 9;
						}
						else
						{
							if ((trafficLightData.m_Type & TrafficLightType.CrossingLeft) != 0)
							{
								continue;
							}
							num7--;
						}
					}
					else if ((trafficLightData.m_Type & (TrafficLightType.CrossingLeft | TrafficLightType.CrossingRight)) != 0)
					{
						continue;
					}
					if (num7 <= 0)
					{
						continue;
					}
					num3 += (float)(50 * num7);
					if ((trafficLightData.m_Type & TrafficLightType.VehicleLeft) != 0)
					{
						ObjectGeometryData objectGeometryData = _manager.GetComponentData<ObjectGeometryData>(entity);
						Bounds1 bounds = trafficLightData.m_ReachOffset - objectGeometryData.m_Bounds.min.x;
						if (bounds.min > math.max(trafficSignNeeds.m_VehicleLanesLeft, 1f))
						{
							continue;
						}
						bounds = trafficSignNeeds.m_VehicleLanesLeft - bounds;
						num3 -= 50f * math.max(0f, math.max(0f - bounds.min, bounds.max));
					}
					if ((trafficLightData.m_Type & TrafficLightType.VehicleRight) != 0)
					{
						ObjectGeometryData objectGeometryData2 = _manager.GetComponentData<ObjectGeometryData>(entity);
						Bounds1 bounds2 = trafficLightData.m_ReachOffset + objectGeometryData2.m_Bounds.max.x;
						if (bounds2.min > math.max(trafficSignNeeds.m_VehicleLanesRight, 1f))
						{
							continue;
						}
						bounds2 = trafficSignNeeds.m_VehicleLanesRight - bounds2;
						num3 -= 50f * math.max(0f, math.max(0f - bounds2.min, bounds2.max));
					}
				}
				if (_manager.HasComponent<LaneDirectionData>(entity))
				{
					LaneDirectionData laneDirectionData = _manager.GetComponentData<LaneDirectionData>(entity);
					if (trafficSignNeeds.m_Left == LaneDirectionType.None && trafficSignNeeds.m_Forward == LaneDirectionType.None && trafficSignNeeds.m_Right == LaneDirectionType.None)
					{
						continue;
					}
					int num8 = 0;
					num8 += 180 - math.abs(trafficSignNeeds.m_Left - laneDirectionData.m_Left);
					num8 += 180 - math.abs(trafficSignNeeds.m_Forward - laneDirectionData.m_Forward);
					num8 += 180 - math.abs(trafficSignNeeds.m_Right - laneDirectionData.m_Right);
					num3 += (float)num8 * 0.1f;
				}
				SpawnableObjectData spawnableObjectData = _manager.GetComponentData<SpawnableObjectData>(entity);
				Entity entity2 = ((spawnableObjectData.m_RandomizationGroup != Entity.Null) ? spawnableObjectData.m_RandomizationGroup : entity);
				Unity.Mathematics.Random random4 = random;
				random.NextInt();
				random.NextInt();
				if (_groups.TryGetValue((entity2, groupIndex), out var item2))
				{
					num3 += 0.5f;
					random4 = item2;
				}
				if (num3 > num)
				{
					num = num3;
					prefab2 = entity;
					groupPrefab2 = entity2;
					random3 = random4;
					flag = flag2;
					num2 = _manager.GetComponentData<SpawnableObjectData>(entity).m_Probability;
				}
				else if (num3 == num)
				{
					int probability2 = _manager.GetComponentData<SpawnableObjectData>(entity).m_Probability;
					num2 += probability2;
					if (random.NextInt(num2) < probability2)
					{
						prefab2 = entity;
						groupPrefab2 = entity2;
						random3 = random4;
						flag = flag2;
					}
				}
			}
			if (num2 <= 0)
			{
				return false;
			}
			_groups.TryAdd((groupPrefab2, groupIndex), random3);
			if (random3.NextInt(100) < probability)
			{
				selection = new Selection(prefab2, groupIndex, random3, flag);
return true;
			}
		
        return false;
    }

    private bool CheckRequirements(Entity prefab, bool isExplicit)
    {

			if (TryBuffer<ObjectRequirementElement>(prefab, out var bufferData))
			{
				
				int num = -1;
				bool flag = true;
				for (int i = 0; i < bufferData.Length; i++)
				{
					ObjectRequirementElement objectRequirementElement = bufferData[i];
					if ((objectRequirementElement.m_Type & ObjectRequirementType.SelectOnly) != 0)
					{
						continue;
					}
					if (objectRequirementElement.m_Group != num)
					{
						if (!flag)
						{
							break;
						}
						num = objectRequirementElement.m_Group;
						flag = false;
					}
					if (objectRequirementElement.m_Requirement != Entity.Null)
					{
						flag |= objectRequirementElement.m_Requirement == _theme || (isExplicit && (objectRequirementElement.m_Type & ObjectRequirementType.IgnoreExplicit) != 0);
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		
    }

    private bool TryRead<T>(Entity entity, out T data) where T : unmanaged, IComponentData
    {
        if (!_manager.HasComponent<T>(entity)) { data = default; return false; }
        data = _manager.GetComponentData<T>(entity); return true;
    }

    private bool TryBuffer<T>(Entity entity, out DynamicBuffer<T> data) where T : unmanaged, IBufferElementData
    {
        if (!_manager.HasBuffer<T>(entity)) { data = default; return false; }
        data = _manager.GetBuffer<T>(entity, true); return true;
    }
}
