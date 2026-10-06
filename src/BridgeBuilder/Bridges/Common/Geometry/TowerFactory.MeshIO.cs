

using Colossal.AssetPipeline.Importers;





using System;
using System.Collections.Generic;
using System.Globalization;

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    /// <summary>
    /// One mesh turned into one model: the widened positions, and every other channel carried across
    /// byte for byte in the format the source declared it in.
    ///
    /// The formats are the point, and getting them wrong is what drew the cables as shards lying over
    /// the deck. A net piece declares:
    ///
    ///     Position:Float32x3@0  Normal:SNorm16x2@1  Tangent:Float32x1@1  TexCoord0:Float16x2@1
    ///
    /// Two components of signed normalised sixteen-bit for a normal, because it is octahedrally packed;
    /// one float for a tangent, because it is an angle about that normal. Reading them back through
    /// Unity's convenience accessors gives unpacked Vector3 and Vector4, and writing those out declares
    /// three and four floats where the shader expects two shorts and one float. The renderer walks each
    /// vertex at a stride it computes from the declared layout, so from the first vertex onward every
    /// channel is read from the wrong place. Positions survive - they are Float32x3 either way and come
    /// first - which is why the geometry was the right size and everything about it was wrong.
    ///
    /// So nothing is re-encoded. The raw vertex buffer is read, each attribute is lifted out of its
    /// stream at the offset and width the mesh says it occupies, and handed on unchanged. Only the
    /// positions are rewritten, and only because they are the one thing this is meant to change.
    /// </summary>
    private static ModelImporter.Model? BuildModel(
        string name, Mesh mesh, Vector3[] vertices, out string? error,
        bool[]? dropped = null)
    {
        error = null;
        var attributes = new List<ModelImporter.Model.VertexData>();
        var declared = mesh.GetVertexAttributes();

        // Refuse an unsupported layout before allocating any persistent buffers. This used to abort
        // from the game export path; now the caller records the failure and omits the incomplete
        // derived prefab without unwinding through the simulation update.
        foreach (var attribute in declared)
        {
            if (FormatSize(attribute.format) == 0)
            {
                error = string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' uses unsupported vertex format {1}; the current derived prefab was "
                    + "stopped without publishing a partial mesh.",
                    name, attribute.format);
                return null;
            }

            if (attribute.attribute == VertexAttribute.Position
                && (attribute.format != VertexAttributeFormat.Float32 || attribute.dimension != 3))
            {
                error = string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' stores positions as {1}x{2}, which this exporter cannot rewrite; the "
                    + "current derived prefab was stopped without publishing a partial mesh.",
                    name, attribute.format, attribute.dimension);
                return null;
            }
        }

        Mesh.MeshDataArray read = default;
        try
        {
            read = Mesh.AcquireReadOnlyMeshData(mesh);
            var data = read[0];

            // The raw bytes of each stream, kept as they are. Attributes within a stream are
            // interleaved, so each one is picked out by its own offset and width.
            var streams = new Dictionary<int, byte[]>();
            var strides = new Dictionary<int, int>();
            foreach (var attribute in declared)
            {
                if (streams.ContainsKey(attribute.stream)) continue;

                var raw = data.GetVertexData<byte>(attribute.stream);
                var copy = new byte[raw.Length];
                raw.CopyTo(copy);
                streams[attribute.stream] = copy;
                strides[attribute.stream] = mesh.GetVertexBufferStride(attribute.stream);
            }

            foreach (var attribute in declared)
            {
                var size = FormatSize(attribute.format) * attribute.dimension;
                var offset = mesh.GetVertexAttributeOffset(attribute.attribute);
                var stride = strides[attribute.stream];
                var stream = streams[attribute.stream];

                var bytes = new byte[size * vertices.Length];
                if (attribute.attribute == VertexAttribute.Position)
                {
                    // The one channel that changes. Written in the format the mesh declares for it -
                    // three floats on every mesh seen so far, preflighted above rather than assumed.
                    Buffer.BlockCopy(Flatten(vertices), 0, bytes, 0, bytes.Length);
                }
                else
                {
                    for (var index = 0; index < vertices.Length; index++)
                    {
                        var from = (index * stride) + offset;
                        if (from + size > stream.Length) break;
                        Buffer.BlockCopy(stream, from, bytes, index * size, size);
                    }
                }

                attributes.Add(new ModelImporter.Model.VertexData(
                    attribute.attribute,
                    attribute.format,
                    attribute.dimension,
                    new NativeArray<byte>(bytes, Allocator.Persistent),
                    false));

            }
        }
        finally
        {
            if (read.Length > 0) read.Dispose();
        }

        // The bounds every generated mesh was missing.
        //
        // ModelImporter.Model.ToUnityMesh calls Mesh.SetSubMesh with flags 15, which includes
        // DontRecalculateBounds, and then sets mesh.bounds to the union of the descriptors' own
        // bounds. Nothing else ever computes them. A descriptor built from the three argument
        // constructor leaves that field at its default, so the mesh declared itself a zero-size box
        // at the origin - which is what every mesh this mod has written did, tower and cable alike,
        // while its vertices, indices and vertex layout were all correct. The game's own importers
        // set this field; this did not until it was read out of the dump.
        var points = ToPoints(vertices);
        var indices = new List<int>();
        var subMeshes = new List<SubMeshDescriptor>();
        for (var sub = 0; sub < mesh.subMeshCount; sub++)
        {
            var start = indices.Count;

            // A triangle whose three corners all belong to something being taken off the bridge is not
            // written. That is what taking it off means: the geometry is gone from the index buffer
            // rather than present with no area, which is a thing the renderer still has to carry and
            // the file still has to hold.
            //
            // All three corners, so that a triangle bridging the railing and what it stands on is
            // kept and the surface it belongs to is not left with a hole in it.
            var corners = mesh.GetTriangles(sub);
            for (var corner = 0; corner + 2 < corners.Length; corner += 3)
            {
                if (dropped != null
                    && corners[corner] < dropped.Length
                    && corners[corner + 1] < dropped.Length
                    && corners[corner + 2] < dropped.Length
                    && dropped[corners[corner]]
                    && dropped[corners[corner + 1]]
                    && dropped[corners[corner + 2]])
                {
                    continue;
                }

                indices.Add(corners[corner]);
                indices.Add(corners[corner + 1]);
                indices.Add(corners[corner + 2]);
            }

            var count = indices.Count - start;

            TowerWidening.ExtentOf(points, indices, start, count, out var low, out var high);
            TowerWidening.IndexRangeOf(indices, start, count, out var first, out var used);

            var centre = (low + high) * 0.5f;
            var size = high - low;
            subMeshes.Add(new SubMeshDescriptor(start, count, MeshTopology.Triangles)
            {
                bounds = new Bounds(
                    new Vector3(centre.x, centre.y, centre.z),
                    new Vector3(size.x, size.y, size.z)),
                baseVertex = 0,
                firstVertex = first,
                vertexCount = used,
            });
        }

        return new ModelImporter.Model(
            name,
            Matrix4x4.identity,
            vertices.Length,
            new NativeArray<int>(indices.ToArray(), Allocator.Persistent),
            attributes.ToArray(),
            subMeshes.ToArray(),
            -1,
            Array.Empty<ModelImporter.Model.BoneInfo>());
    }

    /// <summary>How many bytes one component of a vertex channel takes.</summary>
    private static int FormatSize(VertexAttributeFormat format)
    {
        switch (format)
        {
            case VertexAttributeFormat.Float32:
            case VertexAttributeFormat.UInt32:
            case VertexAttributeFormat.SInt32:
                return 4;
            case VertexAttributeFormat.Float16:
            case VertexAttributeFormat.UNorm16:
            case VertexAttributeFormat.SNorm16:
            case VertexAttributeFormat.UInt16:
            case VertexAttributeFormat.SInt16:
                return 2;
            case VertexAttributeFormat.UNorm8:
            case VertexAttributeFormat.SNorm8:
            case VertexAttributeFormat.UInt8:
            case VertexAttributeFormat.SInt8:
                return 1;
            default:
                return 0;
        }
    }




    private static float[] Flatten(Vector3[] values)
    {
        var result = new float[values.Length * 3];
        for (var index = 0; index < values.Length; index++)
        {
            result[index * 3] = values[index].x;
            result[(index * 3) + 1] = values[index].y;
            result[(index * 3) + 2] = values[index].z;
        }

        return result;
    }


    private static float3[] ToPoints(Vector3[] vertices)
    {
        var points = new float3[vertices.Length];
        for (var index = 0; index < vertices.Length; index++)
        {
            points[index] = new float3(vertices[index].x, vertices[index].y, vertices[index].z);
        }

        return points;
    }

    private static Vector3[] ToVectors(float3[] points)
    {
        var vertices = new Vector3[points.Length];
        for (var index = 0; index < points.Length; index++)
        {
            vertices[index] = new Vector3(points[index].x, points[index].y, points[index].z);
        }

        return vertices;
    }

    private static uint CountIndices(Mesh mesh)
    {
        var total = 0u;
        for (var sub = 0; sub < mesh.subMeshCount; sub++) total += mesh.GetIndexCount(sub);
        return total;
    }

}
