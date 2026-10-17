using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRoadObjects
{
    internal sealed partial class ObjectCompositionSystem
    {
        internal (int Occurrence, int RuleIndex)[] TraceOrigins(Entity composition, NetCompositionData data)
        {
            if (World == World.DefaultGameObjectInjectionWorld)
                throw new InvalidOperationException("Object provenance requires a private composition world.");
            using var pieces = EntityManager.GetBuffer<NetCompositionPiece>(composition, true).ToNativeArray(Allocator.Temp);
            using var expected = EntityManager.GetBuffer<NetCompositionObject>(composition, true).ToNativeArray(Allocator.Temp);
            var trace = EntityManager.CreateEntity();
            try
            {
                EntityManager.AddBuffer<NetCompositionPiece>(trace);
                EntityManager.AddBuffer<NetCompositionObject>(trace);
                EntityManager.AddBuffer<NetPieceObject>(trace);
                var tags = new List<(int Occurrence, int RuleIndex, float Longitudinal)>();
                for (var occurrence = 0; occurrence < pieces.Length; occurrence++)
                {
                    var piece = pieces[occurrence];
                    if (!EntityManager.HasBuffer<NetPieceObject>(piece.m_Piece)) continue;
                    using var rules = EntityManager.GetBuffer<NetPieceObject>(piece.m_Piece, true).ToNativeArray(Allocator.Temp);
                    var inverted = (piece.m_SectionFlags & NetSectionFlags.Invert) != 0;
                    // AddCompositionObjects reads only NetPieceObject from the piece
                    // entity. A one-rule proxy preserves the original object entity,
                    // composition flags and offsets; native grouping still sees the
                    // complete accumulated output from every preceding rule.
                    piece.m_Piece = trace;
                    var input = EntityManager.GetBuffer<NetCompositionPiece>(trace);
                    input.Clear(); input.Add(piece);
                    for (var step = 0; step < rules.Length; step++)
                    {
                        // Native inverted pieces visit the original rules in reverse.
                        // Keep their flags unchanged so native position/rotation inversion
                        // still happens exactly once inside the one-rule invocation.
                        var ruleIndex = inverted ? rules.Length - step - 1 : step;
                        var proxy = EntityManager.GetBuffer<NetPieceObject>(trace);
                        proxy.Clear(); proxy.Add(rules[ruleIndex]);
                        Compose(trace, data);
                        var output = EntityManager.GetBuffer<NetCompositionObject>(trace);
                        for (var i = 0; i < output.Length; i++)
                        {
                            var item = output[i];
                            if (float.IsNaN(item.m_Position.y)) continue;
                            if (!float.IsFinite(item.m_Position.y))
                                throw new InvalidOperationException("A native object rule has a nonfinite longitudinal position.");
                            if (tags.Count >= 0x00400000)
                                throw new InvalidOperationException("Object provenance exceeded its trace identity range.");
                            tags.Add((occurrence, ruleIndex, item.m_Position.y));
                            // Native grouping never reads Position.y on existing entries.
                            // Tag only the private trace after each native rule invocation.
                            item.m_Position.y = BitConverter.Int32BitsToSingle(0x7fc00000 | (tags.Count - 1));
                            output[i] = item;
                        }
                    }
                }
                var actual = EntityManager.GetBuffer<NetCompositionObject>(trace);
                if (actual.Length != expected.Length)
                    throw new InvalidOperationException("Object provenance changed native composition length.");
                var result = new (int Occurrence, int RuleIndex)[actual.Length];
                for (var i = 0; i < result.Length; i++)
                {
                    var item = actual[i];
                    var bits = BitConverter.SingleToInt32Bits(item.m_Position.y);
                    var id = bits & 0x003fffff;
                    if ((bits & unchecked((int)0xffc00000)) != 0x7fc00000 || id >= tags.Count)
                        throw new InvalidOperationException("Native object provenance lost a trace identity.");
                    var origin = tags[id];
                    item.m_Position.y = origin.Longitudinal;
                    // Restore then compare every field. Trace output is never used as the
                    // actual placement; only verified source occurrence/rule indices escape.
                    if (!item.Equals(expected[i]))
                        throw new InvalidOperationException("Object provenance changed native composition fields or order.");
                    result[i] = (origin.Occurrence, origin.RuleIndex);
                }
                return result;
            }
            finally { EntityManager.DestroyEntity(trace); }
        }
    }
}
