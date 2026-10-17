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

// Position construction from native 1.6.2f1 CreateSecondaryNodeObjects. A fixed-size
// crossing is a node; ordinary edge spacing is not its attachment placement rule.
internal static class MovableNodeObjectPlacement
{
    // Priority is the native candidate curve length used for streetlight arbitration.
    // The callback must retain native sign/light/utility selection and its random order.
    internal delegate void ResolveCandidate(MovableRoadObjectPlacement.Candidate candidate,
        float priority, bool lowered, ref Random random);

    // One non-middle connected-edge contribution. The caller supplies its actual node
    // composition and EdgeNodeGeometry; this method neither invents an endpoint nor
    // treats candidate enumeration as final object selection.
    internal static void Visit(IReadOnlyList<MovableRoadObjects.Rule> rules,
        EdgeGeometry edge, EdgeNodeGeometry geometry, NetCompositionData composition,
        NetGeometryData network, bool isEnd, ref Random random, ResolveCandidate resolve)
    {
        if (!math.isfinite(network.m_EdgeLengthRange.max) || network.m_EdgeLengthRange.max <= 0f ||
            !math.isfinite(composition.m_Width) || composition.m_Width <= 0f)
            throw new ArgumentException("Node object placement requires initialized native network dimensions.");
        var lowered = ((composition.m_Flags.m_Left | composition.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0;
        // Preserve the installed implementation's tangent choices, including its right
        // end-segment StartTangent; do not silently substitute a symmetric approximation.
        var approachTangent = isEnd
            ? MathUtils.EndTangent(edge.m_End.m_Left) + MathUtils.StartTangent(edge.m_End.m_Right)
            : -(MathUtils.StartTangent(edge.m_Start.m_Left) + MathUtils.StartTangent(edge.m_Start.m_Right));
        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            var item = rule.Placement;
            float num8 = item.m_Position.x / math.max(1f, composition.m_Width) + 0.5f;
            float num9 = composition.m_MiddleOffset / math.max(1f, composition.m_Width) + 0.5f;
            float2 float2 = new float2(math.cmin(item.m_CurveOffsetRange), math.cmax(item.m_CurveOffsetRange));
            if (float2.y > 0.5f)
            {
            	float2 = ((!(float2.x > 0.5f)) ? new float2((float2.x + 1f - float2.y) * 0.5f, 0.5f) : (1f - float2.yx));
            }
            float num10 = math.lerp(float2.x, float2.y, random.NextFloat(1f));
            float num11;
            float3 position;
            float3 float4;
            float3 x3;
            if ((item.m_Flags & (SubObjectFlags.OnMedian | SubObjectFlags.PreserveShape)) != 0)
            {
            	Bezier4x3 middle = geometry.m_Middle;
            	float3 float3;
            	float3 end;
            	if (geometry.m_MiddleRadius > 0f)
            	{
            		float3 = geometry.m_Left.m_Right.a - geometry.m_Left.m_Left.a;
            		end = geometry.m_Right.m_Right.d - geometry.m_Right.m_Left.d;
            	}
            	else
            	{
            		float3 = geometry.m_Right.m_Right.a - geometry.m_Left.m_Left.a;
            		end = geometry.m_Right.m_Right.d - geometry.m_Left.m_Left.d;
            	}
            	if ((item.m_Flags & SubObjectFlags.PreserveShape) != 0)
            	{
            		end = float3;
            	}
            	num11 = MathUtils.Length(middle.xz);
            	if (num11 < 0.01f)
            	{
            		position = middle.a + float3 * (num8 - num9);
            		float4 = ((!(geometry.m_MiddleRadius > 0f)) ? (MathUtils.StartTangent(geometry.m_Left.m_Left) + MathUtils.StartTangent(geometry.m_Right.m_Right)) : (MathUtils.StartTangent(geometry.m_Left.m_Left) + MathUtils.StartTangent(geometry.m_Left.m_Right)));
            		x3 = float4;
            	}
            	else
            	{
            		float4 = MathUtils.StartTangent(middle);
            		float num12 = num10 * 2f * num11 + item.m_Position.y;
            		if (num12 > 0.001f)
            		{
            			Bounds1 t = new Bounds1(0f, 1f);
            			MathUtils.ClampLength(middle.xz, ref t, num12);
            			position = MathUtils.Position(middle, t.max) + math.lerp(float3, end, t.max) * (num8 - num9);
            			x3 = MathUtils.Tangent(middle, t.max);
            		}
            		else
            		{
            			position = middle.a + float3 * (num8 - num9);
            			x3 = float4;
            		}
            	}
            	if (geometry.m_MiddleRadius == 0f && math.all(geometry.m_Left.m_Length <= 0.05f) && math.all(geometry.m_Right.m_Length < 0.05f))
            	{
            		float4 = approachTangent;
            		x3 = approachTangent;
            	}
            }
            else if (geometry.m_MiddleRadius > 0f)
            {
            	MathUtils.Divide(geometry.m_Middle, out var output, out var output2, 0.99f);
            	Bezier4x3 curve;
            	Bezier4x3 curve2;
            	if (num8 >= num9)
            	{
            		curve = LerpRemap(output, geometry.m_Left.m_Right, (num8 - num9) / math.max(1E-05f, 1f - num9), composition.m_SyncVertexOffsetsRight, geometry.m_SyncVertexTargetsRight);
            		curve2 = LerpRemap2(output2, geometry.m_Right.m_Right, (num8 - num9) / math.max(1E-05f, 1f - num9), composition.m_SyncVertexOffsetsRight, geometry.m_SyncVertexTargetsRight);
            	}
            	else
            	{
            		curve = LerpRemap(geometry.m_Left.m_Left, output, num8 / math.max(1E-05f, num9), composition.m_SyncVertexOffsetsLeft, geometry.m_SyncVertexTargetsLeft);
            		curve2 = LerpRemap2(geometry.m_Right.m_Left, output2, num8 / math.max(1E-05f, num9), composition.m_SyncVertexOffsetsLeft, geometry.m_SyncVertexTargetsLeft);
            	}
            	float num13 = MathUtils.Length(curve.xz);
            	float num14 = MathUtils.Length(curve2.xz);
            	num11 = num13 + num14;
            	float4 = MathUtils.StartTangent(curve);
            	float num15 = num10 * 2f * num11 + item.m_Position.y;
            	if (num15 > num13)
            	{
            		Bounds1 t2 = new Bounds1(0f, 1f);
            		MathUtils.ClampLength(curve2.xz, ref t2, num15 - num13);
            		position = MathUtils.Position(curve2, t2.max);
            		x3 = MathUtils.Tangent(curve2, t2.max);
            	}
            	else if (num15 > 0.001f)
            	{
            		Bounds1 t3 = new Bounds1(0f, 1f);
            		MathUtils.ClampLength(curve.xz, ref t3, num15);
            		position = MathUtils.Position(curve, t3.max);
            		x3 = MathUtils.Tangent(curve, t3.max);
            	}
            	else
            	{
            		position = curve.a;
            		x3 = float4;
            	}
            }
            else
            {
            	Bezier4x3 curve3 = ((!(num8 >= num9)) ? LerpRemap(geometry.m_Left.m_Left, geometry.m_Left.m_Right, num8 / math.max(1E-05f, num9), composition.m_SyncVertexOffsetsLeft, geometry.m_SyncVertexTargetsLeft) : LerpRemap(geometry.m_Right.m_Left, geometry.m_Right.m_Right, (num8 - num9) / math.max(1E-05f, 1f - num9), composition.m_SyncVertexOffsetsRight, geometry.m_SyncVertexTargetsRight));
            	num11 = MathUtils.Length(curve3.xz);
            	float4 = MathUtils.StartTangent(curve3);
            	float num16 = num10 * 2f * num11 + item.m_Position.y;
            	if (num16 > 0.001f)
            	{
            		Bounds1 t4 = new Bounds1(0f, 1f);
            		MathUtils.ClampLength(curve3.xz, ref t4, num16);
            		position = MathUtils.Position(curve3, t4.max);
            		x3 = MathUtils.Tangent(curve3, t4.max);
            	}
            	else
            	{
            		position = curve3.a;
            		x3 = float4;
            	}
            	if (math.all(geometry.m_Left.m_Length <= 0.05f) && math.all(geometry.m_Right.m_Length < 0.05f))
            	{
            		float4 = approachTangent;
            		x3 = approachTangent;
            	}
            }
            float4.y = math.lerp(0f, float4.y, item.m_UseCurveRotation.x);
            x3.y = math.lerp(0f, x3.y, item.m_UseCurveRotation.x);
            float4 = math.normalizesafe(float4, new float3(0f, 0f, 1f));
            x3 = math.normalizesafe(x3, float4);
            quaternion q = quaternion.LookRotationSafe(float4, math.up());
            quaternion q2 = quaternion.LookRotationSafe(x3, math.up());
            quaternion rotation = math.slerp(q, q2, item.m_UseCurveRotation.y);
            ObjectTransform transform = new ObjectTransform(position, rotation);
            ObjectTransform transform2 = new ObjectTransform(item.m_Offset, item.m_Rotation);

            if (item.m_Probability < 100)
            {
            	item.m_Probability = math.clamp(Mathf.RoundToInt((float)item.m_Probability * (num11 / network.m_EdgeLengthRange.max)), 1, item.m_Probability);
            }
            resolve(new MovableRoadObjectPlacement.Candidate(index, rule.Prefab, transform,
                transform2, item.m_Flags, item.m_Probability, rule), num11, lowered, ref random);
        }
    }

    private static float Remap(float t, float4 syncOffsets, float4 syncTargets)
    {
    	if (t < syncOffsets.x)
    	{
    		return syncTargets.x * math.saturate(t / syncOffsets.x);
    	}
    	if (t < syncOffsets.y)
    	{
    		return math.lerp(syncTargets.x, syncTargets.y, math.saturate((t - syncOffsets.x) / (syncOffsets.y - syncOffsets.x)));
    	}
    	if (t < syncOffsets.z)
    	{
    		return math.lerp(syncTargets.y, syncTargets.z, math.saturate((t - syncOffsets.y) / (syncOffsets.z - syncOffsets.y)));
    	}
    	if (t < syncOffsets.w)
    	{
    		return math.lerp(syncTargets.z, syncTargets.w, math.saturate((t - syncOffsets.z) / (syncOffsets.w - syncOffsets.z)));
    	}
    	return math.lerp(syncTargets.w, 1f, math.saturate((t - syncOffsets.w) / (1f - syncOffsets.w)));
    }

    private static Bezier4x3 LerpRemap(Bezier4x3 left, Bezier4x3 right, float t, float4 syncOffsets, float4 syncTargets)
    {
    	float t2 = Remap(t, syncOffsets, syncTargets);
    	float num = math.distance(left.a.xz, right.a.xz);
    	float num2 = math.distance(left.d.xz, right.d.xz);
    	float t3 = 0.5f;
    	syncTargets = math.lerp(syncOffsets * num, syncTargets * num2, t3) / math.max(1E-06f, math.lerp(num, num2, t3));
    	t3 = Remap(t, syncOffsets, syncTargets);
    	Bezier4x3 result = default(Bezier4x3);
    	result.a = math.lerp(left.a, right.a, t);
    	result.b = math.lerp(left.b, right.b, t);
    	result.c = math.lerp(left.c, right.c, t3);
    	result.d = math.lerp(left.d, right.d, t2);
    	return result;
    }

    private static Bezier4x3 LerpRemap2(Bezier4x3 left, Bezier4x3 right, float t, float4 syncOffsets, float4 syncTargets)
    {
    	float t2 = Remap(t, syncOffsets, syncTargets);
    	Bezier4x3 result = default(Bezier4x3);
    	result.a = math.lerp(left.a, right.a, t2);
    	result.b = math.lerp(left.b, right.b, t2);
    	result.c = math.lerp(left.c, right.c, t2);
    	result.d = math.lerp(left.d, right.d, t2);
    	return result;
    }
}
