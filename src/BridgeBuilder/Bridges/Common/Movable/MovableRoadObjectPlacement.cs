using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;
using ObjectTransform = Game.Objects.Transform;
using Random = Unity.Mathematics.Random;

namespace BridgeBuilder.Bridges;

/// <summary>
/// The placement portion of native SecondaryObjectSystem.CreateSecondaryEdgeObjects (1.6.2f1).
/// Native theme/placeholder and traffic-sign selection must run at each candidate, sharing the
/// random stream. This does not replace those decisions with a fixed placeholder or seed.
/// </summary>
internal static class MovableRoadObjectPlacement
{
    internal readonly struct Candidate
    {
        internal readonly int RuleIndex;
        internal readonly ObjectPrefab Prefab;
        internal readonly ObjectTransform Parent, Local, Object;
        internal readonly SubObjectFlags Flags;
        internal readonly int Probability;
        // Keep the original producer through placeholder selection and node-light
        // arbitration. Utility/lane-derived candidates have no composition rule.
        internal readonly MovableRoadObjects.Rule? SourceRule;

        internal Candidate(int ruleIndex, ObjectPrefab prefab, ObjectTransform parent,
            ObjectTransform local, SubObjectFlags flags, int probability, MovableRoadObjects.Rule? sourceRule = null)
            : this(ruleIndex, prefab, parent, local, ObjectUtils.LocalToWorld(parent, local), flags, probability, sourceRule) { }

        // Native utility-node emission supplies an explicit world transform separately
        // from its local elevation/cache transform; composing the two would add height twice.
        internal Candidate(int ruleIndex, ObjectPrefab prefab, ObjectTransform parent,
            ObjectTransform local, ObjectTransform objectTransform, SubObjectFlags flags, int probability,
            MovableRoadObjects.Rule? sourceRule = null)
        {
            RuleIndex = ruleIndex; Prefab = prefab; Parent = parent; Local = local;
            Object = objectTransform;
            Flags = flags; Probability = probability;
            SourceRule = sourceRule;
        }
    }

    internal delegate void ResolveCandidate(Candidate candidate, ref Random random);

    // The caller supplies actual closed-span endpoint geometry/flags. In particular, a node's
    // contribution to even spacing must not be guessed from the bridge's node offset.
    internal static void Visit(IReadOnlyList<MovableRoadObjects.Rule> rules,
        EdgeGeometry edge, NetCompositionData composition, NetGeometryData geometry,
        NetCompositionData start, NetCompositionData end, EdgeNodeGeometry startGeometry,
        EdgeNodeGeometry endGeometry, ref Random random, ResolveCandidate resolve)
    {
        if (!math.isfinite(geometry.m_EdgeLengthRange.max) || geometry.m_EdgeLengthRange.max <= 0f ||
            !math.isfinite(composition.m_Width) || composition.m_Width <= 0f)
            throw new ArgumentException("Road object placement requires initialized native network dimensions.");
        for (var ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
        {
            var rule = rules[ruleIndex];
            var item = rule.Placement;
            var length = edge.m_Start.middleLength + edge.m_End.middleLength;
            var offset = item.m_Position.y;
            var count = 1;
            var even = (item.m_Flags & SubObjectFlags.EvenSpacing) != 0;
            if (even)
            {
                if ((start.m_Flags.m_General & item.m_SpacingIgnore) == 0)
                {
                    var extension = (startGeometry.m_Left.middleLength + startGeometry.m_Right.middleLength) * .5f;
                    extension = math.min(extension, item.m_Spacing * (1f / 3f));
                    length += extension;
                    offset -= extension;
                }
                if ((end.m_Flags.m_General & item.m_SpacingIgnore) == 0)
                {
                    var extension = (endGeometry.m_Left.middleLength + endGeometry.m_Right.middleLength) * .5f;
                    length += math.min(extension, item.m_Spacing * (1f / 3f));
                }
            }
            if (length < item.m_MinLength) continue;
            // These are the native spacing thresholds, not authored-part spatial classifiers.
            if (item.m_Spacing > .1f)
            {
                count = Mathf.FloorToInt(length / item.m_Spacing + .5f);
                count = even ? count - 1 : math.select(count, 1, (count == 0) & (length > item.m_Spacing * .1f));
                if (item.m_AvoidSpacing > .1f)
                {
                    var avoid = Mathf.FloorToInt(length / item.m_AvoidSpacing + .5f);
                    avoid = even ? avoid - 1 : math.select(avoid, 1, (avoid == 0) & (length > item.m_AvoidSpacing * .1f));
                    if ((count & 1) == (avoid & 1))
                    {
                        var choices = count + new int2(-1, 1);
                        var errors = math.abs((float2)choices * item.m_Spacing - length);
                        count = math.select(choices.x, choices.y, errors.y < errors.x || choices.x == 0);
                    }
                }
            }
            if (count <= 0) continue;
            var transverse = item.m_Position.x / math.max(1f, composition.m_Width) + .5f;
            var firstCurve = MathUtils.Lerp(edge.m_Start.m_Left, edge.m_Start.m_Right, transverse);
            var secondCurve = MathUtils.Lerp(edge.m_End.m_Left, edge.m_End.m_Right, transverse);
            var phase = math.lerp(item.m_CurveOffsetRange.x, item.m_CurveOffsetRange.y, random.NextFloat(1f));
            var spacing = length / count;
            if (even) { phase += .5f; spacing = length / (count + 1); }
            for (var index = 0; index < count; index++)
            {
                var distance = (index + phase) * spacing + offset;
                float3 position, endTangent, tangent;
                var range = new Bounds1(0f, 1f);
                if (distance > edge.m_Start.middleLength)
                {
                    MathUtils.ClampLength(MathUtils.Lerp(edge.m_End.m_Left, edge.m_End.m_Right, .5f).xz,
                        ref range, distance - edge.m_Start.middleLength);
                    position = MathUtils.Position(secondCurve, range.max);
                    endTangent = MathUtils.EndTangent(secondCurve);
                    tangent = MathUtils.Tangent(secondCurve, range.max);
                }
                else
                {
                    MathUtils.ClampLength(MathUtils.Lerp(edge.m_Start.m_Left, edge.m_Start.m_Right, .5f).xz,
                        ref range, distance);
                    position = MathUtils.Position(firstCurve, range.max);
                    endTangent = MathUtils.StartTangent(firstCurve);
                    tangent = MathUtils.Tangent(firstCurve, range.max);
                }
                endTangent.y = math.lerp(0f, endTangent.y, item.m_UseCurveRotation.x);
                tangent.y = math.lerp(0f, tangent.y, item.m_UseCurveRotation.x);
                endTangent = math.normalizesafe(endTangent, new float3(0f, 0f, 1f));
                tangent = math.normalizesafe(tangent, endTangent);
                var rotation = math.slerp(quaternion.LookRotationSafe(endTangent, math.up()),
                    quaternion.LookRotationSafe(tangent, math.up()), item.m_UseCurveRotation.y);
                // Preserve the native in-loop probability update, including its rounding order.
                if (item.m_Probability < 100)
                    item.m_Probability = math.clamp(Mathf.RoundToInt(item.m_Probability *
                        (length / geometry.m_EdgeLengthRange.max)), 1, item.m_Probability);
                resolve(new Candidate(ruleIndex, rule.Prefab, new ObjectTransform(position, rotation),
                    new ObjectTransform(item.m_Offset, item.m_Rotation), item.m_Flags, item.m_Probability, rule), ref random);
            }
        }
    }
}
