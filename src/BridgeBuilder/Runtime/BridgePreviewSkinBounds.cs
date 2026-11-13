using System;
using System.Collections.Generic;
using BridgeBuilder.Bridges;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Runtime;

// Only indexed vertices contribute. Transform each vertex with its own native bone,
// never the complete mesh with every bone. Bulk transforms/reduction run on the GPU.
internal static class BridgePreviewSkinBounds
{
    internal static (Bounds Local, Bounds View) Measure(Mesh mesh, Matrix4x4[] skin, Matrix4x4 view)
    {
        var vertices = mesh.vertices;
        var bones = new uint[vertices.Length];
        using (var data = Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var stream = mesh.GetVertexAttributeStream(VertexAttribute.BlendIndices);
            var stride = mesh.GetVertexBufferStride(stream);
            var offset = mesh.GetVertexAttributeOffset(VertexAttribute.BlendIndices);
            var bytes = data[0].GetVertexData<byte>(stream);
            for (var i = 0; i < bones.Length; i++)
            {
                var k = i * stride + offset;
                bones[i] = (uint)(bytes[k] | bytes[k+1] << 8 | bytes[k+2] << 16 | bytes[k+3] << 24);
            }
        }
        var indices = new List<int>();
        for (var slot = 0; slot < mesh.subMeshCount; slot++) indices.AddRange(mesh.GetIndices(slot));
        using var positions = new ComputeBuffer(vertices.Length, 12);
        using var boneBuffer = new ComputeBuffer(bones.Length, 4);
        using var indexBuffer = new ComputeBuffer(indices.Count, 4);
        using var matrices = new ComputeBuffer(skin.Length, 64);
        using var extents = new ComputeBuffer(12, 4);
        positions.SetData(vertices); boneBuffer.SetData(bones); indexBuffer.SetData(indices);
        matrices.SetData(skin);
        var values = new uint[12];
        for (var i = 0; i < 12; i++) values[i] = i % 6 < 3 ? uint.MaxValue : 0;
        extents.SetData(values);
        var shader = MovableGpuBake.CreateProgram();
        try
        {
            var kernel = shader.FindKernel("PreviewBounds");
            shader.SetBuffer(kernel, "PreviewPositions", positions);
            shader.SetBuffer(kernel, "PreviewBones", boneBuffer);
            shader.SetBuffer(kernel, "PreviewIndices", indexBuffer);
            shader.SetBuffer(kernel, "PreviewSkin", matrices);
            shader.SetBuffer(kernel, "PreviewExtents", extents);
            shader.SetInt("PreviewIndexCount", indices.Count);
            shader.SetMatrix("PreviewView", view);
            shader.Dispatch(kernel, (indices.Count + 63) / 64, 1, 1);
            extents.GetData(values);
            return (Read(0), Read(6));
        }
        finally { UnityEngine.Object.DestroyImmediate(shader); }
        Bounds Read(int start)
        {
            float F(int i)
            {
                var b = values[start+i]; b = (b & 0x80000000) != 0 ? b ^ 0x80000000 : ~b;
                var f = BitConverter.ToSingle(BitConverter.GetBytes(b), 0);
                return f;
            }
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(F(0), F(1), F(2)), new Vector3(F(3), F(4), F(5)));
            return bounds;
        }
    }
}
