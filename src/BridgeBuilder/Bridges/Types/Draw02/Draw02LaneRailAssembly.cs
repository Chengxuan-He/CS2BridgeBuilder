using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using UnityEngine;
using Kind = BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind;

namespace BridgeBuilder.Bridges;
// Original high/low orange rails follow actual native car-lane contacts.
internal static class Draw02LaneRailAssembly
{
    internal static MovableRoadAssets.Level[] Apply(IReadOnlyList<MovableRoadAssets.Level> input, MovableSelectedRoadLayout layout)
    {
        var result = new MovableRoadAssets.Level[input.Count];
        var boundaryCache = new Dictionary<float, MovableRoadLaneLayout.Boundary[]>();
        MovableRoadLaneLayout.Boundary[] Boundaries(float z)
        {
            if (!boundaryCache.TryGetValue(z, out var values))
                boundaryCache.Add(z, values = layout.LaneBoundariesAt(z));
            return values;
        }

        var vertices = new Vector3[Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[0]][];
        var indices = new int[Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[1]][];
        for (int lod = Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[2]; lod < vertices.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(input[lod].Source);
                vertices[lod] = meshes[Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[3]].vertices;
                indices[lod] = meshes[Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[4]].GetIndices(Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[5]);
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        for (int lod = Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[6]; lod < result.Length; lod++)
        {
            var source = input[lod];
            var extra = new List<MovableRoadAssets.Supplement>(source.AdditionalStructures);
            foreach (bool positive in new[]
            {
                false,
                true
            }

            )
            {
                int extentPart = Draw02LaneRails.Part(lod, false, true, positive);
                var extent = Draw02LaneRails.Extent(extentPart);
                var middle = Boundaries((extent.x + extent.y) * Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.F32[0]);
                for (int boundary = Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[11]; boundary < middle.Length; boundary++)
                {
                    int index = boundary;
                    var contact = middle[index];
                    int part = Draw02LaneRails.Part(lod, contact.CarCar, (contact.Left & Kind.Car) != (BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind)(Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[12]), positive);
                    int sourceLod = Draw02LaneRails.SourceLod(lod);
                    // Retain this rail's assigned reference contact at stations where
                    // that ordinal is absent, without adding cuts or rejecting shape.
                    float Target(float z)
                    {
                        var values = Boundaries(z);
                        return index < values.Length ? values[index].X : contact.X;
                    }
                    var plan = Draw02LaneRails.Create(part, Target, vertices[sourceLod], indices[sourceLod]);
                    extra.Add(new MovableRoadAssets.Supplement(input[sourceLod].Source, Numeric_Types_Draw02_Draw02LaneRailAssembly_cs.Values.I32[13], plan,
                        semanticKey: $"Draw02Rail/{(positive ? "positive" : "negative")}/{boundary}"));
                }
            }

            result[lod] = new MovableRoadAssets.Level(source.Source, source.Triangles, source.Surfaces, source.Partition!, source.RenderBounds, source.RoadToObject, source.Structure, source.Skeleton, source.Lanes, source.ClosedWorld, source.AppearancePageSize, source.TexelsPerMeter, source.RoadPartitions, source.LanePartitions, source.RoadTrianglePartitions, source.ExtraStructure, extra);
        }

        return result;
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02LaneRailAssembly_cs
internal static class Numeric_Types_Draw02_Draw02LaneRailAssembly_cs
{
    static Numeric_Types_Draw02_Draw02LaneRailAssembly_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw02_Draw02LaneRailAssembly_cs");
}
