using System;
using Colossal.Mathematics;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ObjectStack = Game.Objects.Stack;

namespace BridgeBuilder.Runtime;

/// <summary>
/// ObjectInitializeSystem's authored bounds and AlignSystem's flat-ground
/// placement, followed by the game's actual stack batching helpers. This is
/// instance presentation data only: no prefab or source vertex is modified.
/// </summary>
internal sealed class BridgePreviewObjectLayout
{
    internal Bounds3 Bounds;
    internal float3 Size;
    internal float PlacementOffset;
    internal StackData StackData;
    internal bool HasStack;

    internal static bool TryCreate(ObjectGeometryPrefab prefab, out BridgePreviewObjectLayout layout)
    {
        layout = new BridgePreviewObjectLayout();
        // Already registered objects use the initialized game data verbatim.
        // Generated tmp objects have no ECS registration; aggregate their
        // authored RenderPrefab bounds by the same initialization rules below.
        var world = World.DefaultGameObjectInjectionWorld;
        var prefabs = world?.GetExistingSystemManaged<PrefabSystem>();
        if (prefabs != null && prefabs.TryGetEntity(prefab, out var entity) &&
            world!.EntityManager.HasComponent<ObjectGeometryData>(entity))
        {
            var manager = world.EntityManager;
            var geometry = manager.GetComponentData<ObjectGeometryData>(entity);
            layout.Bounds = geometry.m_Bounds;
            layout.Size = geometry.m_Size;
            if (manager.HasComponent<PlaceableObjectData>(entity))
                layout.PlacementOffset = manager.GetComponentData<PlaceableObjectData>(entity).m_PlacementOffset.y;
            layout.HasStack = manager.HasComponent<StackData>(entity);
            if (layout.HasStack) layout.StackData = manager.GetComponentData<StackData>(entity);
            return true;
        }

        layout.Bounds = new Bounds3(float.MaxValue, float.MinValue);
        layout.StackData.m_FirstBounds = new Bounds1(float.MaxValue, float.MinValue);
        layout.StackData.m_MiddleBounds = new Bounds1(float.MaxValue, float.MinValue);
        layout.StackData.m_LastBounds = new Bounds1(float.MaxValue, float.MinValue);
        var standing = prefab.TryGet<StandingObject>(out var standingObject);
        var found = false;
        // Inactive-state meshes still participate in native bounds aggregation.
        foreach (var part in prefab.m_Meshes ?? Array.Empty<ObjectMeshInfo>())
        {
            if (part?.m_Mesh is not RenderPrefab render) continue;
            var bounds = render.bounds;
            if (prefab.m_Circular || part.m_Rotation.Equals(quaternion.identity))
                bounds += part.m_Position;
            else
                bounds = MathUtils.Bounds(MathUtils.Box(bounds, part.m_Rotation, part.m_Position));
            if (render.TryGet<StackProperties>(out var stack) && stack.m_Direction != StackDirection.None)
            {
                if (layout.HasStack && layout.StackData.m_Direction != stack.m_Direction)
                {
                    return false;
                }
                layout.HasStack = true;
                layout.StackData.m_Direction = stack.m_Direction;
                if (standing && stack.m_Direction == StackDirection.Up && stack.m_Order == StackOrder.Last)
                    bounds.max.y = math.max(bounds.max.y, standingObject.m_LegSize.y + .1f);
                var axis = Axis(stack.m_Direction);
                var range = new Bounds1(bounds.min[axis] + stack.m_StartOverlap,
                    bounds.max[axis] - stack.m_EndOverlap);
                switch (stack.m_Order)
                {
                    case StackOrder.First:
                        layout.StackData.m_FirstBounds |= range;
                        layout.StackData.m_DontScale.x |= stack.m_ForbidScaling;
                        break;
                    case StackOrder.Middle:
                        layout.StackData.m_MiddleBounds |= range;
                        layout.StackData.m_DontScale.y |= stack.m_ForbidScaling;
                        break;
                    case StackOrder.Last:
                        layout.StackData.m_LastBounds |= range;
                        layout.StackData.m_DontScale.z |= stack.m_ForbidScaling;
                        break;
                }
                bounds.min[axis] = stack.m_Order == StackOrder.First ? math.min(bounds.min[axis], 0f) : 0f;
                bounds.max[axis] = stack.m_Order == StackOrder.Last ? math.max(bounds.max[axis], 0f) : 0f;
            }
            if (standing) bounds.max.y = math.max(bounds.max.y, standingObject.m_LegSize.y + .1f);
            layout.Bounds |= bounds;
            found = true;
        }
        if (!found) return false;
        ResetEmpty(ref layout.StackData.m_FirstBounds);
        ResetEmpty(ref layout.StackData.m_MiddleBounds);
        ResetEmpty(ref layout.StackData.m_LastBounds);
        layout.Size = ObjectUtils.GetSize(layout.Bounds);
        if (prefab.TryGet<PillarObject>(out var pillar)) layout.PlacementOffset = pillar.m_AnchorOffset;
        return true;
    }

    internal ObjectStack Align(ref Vector3 position, bool anchorTop, bool anchorCenter,
        bool onGround, float groundY)
    {
        if (anchorTop) position.y -= Bounds.max.y - PlacementOffset;
        else if (anchorCenter) position.y -= (Bounds.max.y - Bounds.min.y) * .5f;
        var stack = InitialStack();
        if (HasStack)
        {
            // AlignSystem.AlignHeight after anchoring. Extend the first/middle
            // parts down to terrain; leave the top connected to the bridge.
            stack.m_Range = new Bounds1((onGround ? groundY - position.y : 0f) + Bounds.min.y, Bounds.max.y);
            BatchDataHelpers.AlignStack(ref stack, StackData, start: false, end: true);
        }
        else if (onGround && !anchorTop && !anchorCenter) position.y = groundY;
        return stack;
    }

    internal ObjectStack InitialStack(float elevation = 0f)
    {
        var data = StackData;
        return new ObjectStack
        {
            m_Range = data.m_Direction == StackDirection.Up
                ? new Bounds1(data.m_FirstBounds.min - elevation, data.m_LastBounds.max)
                : new Bounds1(data.m_FirstBounds.min, data.m_FirstBounds.max +
                    MathUtils.Size(data.m_MiddleBounds) * 2f + MathUtils.Size(data.m_LastBounds))
        };
    }

    internal static float ChildElevation(float ownerElevation, float localY)
    {
        var elevation = ownerElevation + localY;
        // SubObjectSystem's explicit near-ground placement rule, not a mesh
        // classification threshold. Stacked parent codes do not change it.
        return ownerElevation >= 0f && elevation >= -.5f && elevation < 0f ? 0f : elevation;
    }

    internal static SubMeshFlags Flags(RenderPrefab render)
    {
        if (!render.TryGet<StackProperties>(out var stack) || stack.m_Direction == StackDirection.None) return 0;
        return stack.m_Order == StackOrder.First ? SubMeshFlags.IsStackStart :
            stack.m_Order == StackOrder.Last ? SubMeshFlags.IsStackEnd : SubMeshFlags.IsStackMiddle;
    }

    private static int Axis(StackDirection direction)
        => direction == StackDirection.Right ? 0 : direction == StackDirection.Forward ? 2 : 1;

    private static void ResetEmpty(ref Bounds1 bounds)
    {
        if (bounds.min > bounds.max) bounds = default;
    }
}
