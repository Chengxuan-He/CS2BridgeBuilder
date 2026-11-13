using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Evaluates a native straight-edge composition mesh before binding it to mechanism bones.
/// The selectors and UV rule follow the installed NetCompositionMeshLit vertex program;
/// no composition matrix or matrix-selector color is required by the resulting road vertices.
/// Material conversion is a separate mandatory step, not established by this evaluation.
/// </summary>
internal static class MovableStraightRoadEvaluation
{
    internal static bool TryEvaluate(Mesh mesh, float width, float length, int occurrence,
        out MovableRoadGeometry.Triangle[] triangles, out string? error)
        => TryEvaluate(mesh, width, length, occurrence, null, out triangles, out error);

    internal static bool TryEvaluate(Mesh mesh, BatchDataHelpers.CompositionParameters parameters,
        float3 origin, int occurrence, out MovableRoadGeometry.Triangle[] triangles, out string? error)
        => TryEvaluate(mesh, 0f, 0f, occurrence, new MovableCompositionDeformation(parameters, origin),
            out triangles, out error);

    internal sealed class Evaluation
    {
        internal readonly MovableRoadGeometry.Triangle[] Triangles;
        internal readonly string? Error;
        internal Evaluation(string error){Error=error;Triangles=Array.Empty<MovableRoadGeometry.Triangle>();}
        internal Evaluation(MovableRoadGeometry.Triangle[] triangles){Triangles=triangles;}
    }
    internal static Task<Evaluation> EvaluateAsync(Mesh mesh, BatchDataHelpers.CompositionParameters parameters,
        float3 origin,int occurrence)=>EvaluateAsync(mesh,0f,0f,occurrence,new MovableCompositionDeformation(parameters,origin));

    private static bool TryEvaluate(Mesh mesh,float width,float length,int occurrence,
        MovableCompositionDeformation? deformation,out MovableRoadGeometry.Triangle[] triangles,out string? error)
    {
        var evaluated=GenerationWork.Synchronous(()=>EvaluateAsync(mesh,width,length,occurrence,deformation));
        triangles=evaluated.Triangles;error=evaluated.Error;return error==null;
    }
    private static async Task<Evaluation> EvaluateAsync(Mesh mesh,float width,float length,int occurrence,
        MovableCompositionDeformation? deformation)
    {
        string? error;
        try
        {
            Vector3[] positions; Vector4[] textureCoordinates; MovablePackedFrame.Snapshot packedNormals,packedTangents;
            Color32[] selectors; int[][] indices;
            {
            using var raw = Mesh.AcquireReadOnlyMeshData(mesh);
            var streams=new Dictionary<int,byte[]>();
            packedNormals=MovablePackedFrame.Read(mesh,raw[0],VertexAttribute.Normal,streams);
            packedTangents=MovablePackedFrame.Read(mesh,raw[0],VertexAttribute.Tangent,streams);
            positions = mesh.vertices; selectors = mesh.colors32;
            var uv = new List<Vector4>(); mesh.GetUVs(0, uv);
            if (positions.Length != mesh.vertexCount || selectors.Length != mesh.vertexCount || uv.Count != mesh.vertexCount)
            { error = "The road composition mesh has incomplete vertex channels."; return new Evaluation(error); }
            indices = new int[mesh.subMeshCount][];
            for(int slot=0;slot<indices.Length;slot++)
            {
                if(mesh.GetTopology(slot)!=MeshTopology.Triangles)
                    return new Evaluation($"Road material slot {slot} uses unsupported primitive topology.");
                indices[slot]=mesh.GetIndices(slot);
            }
            textureCoordinates=uv.ToArray();
            }
            return await GenerationWork.Background("road composition deform and expand",()=>
            {
                var decoded=MovablePackedFrame.DecodeSnapshots(packedNormals,packedTangents);
                if(EvaluateData(positions,selectors,textureCoordinates,decoded.Normals,decoded.Tangents,indices,
                    width,length,occurrence,deformation,out var triangles,out var failure))
                    return new Evaluation(triangles);
                return new Evaluation(failure ?? "Road composition evaluation failed.");
            });
        }
        catch(OperationCanceledException){throw;}
        catch(Exception exception){return new Evaluation("Straight road evaluation failed: "+exception.Message);}
    }

    private static bool EvaluateData(Vector3[] positions,Color32[] selectors,Vector4[] uv,
        Vector3[] normals,Vector4[] tangents,int[][] indicesBySlot,float width,float length,int occurrence,
        MovableCompositionDeformation? deformation,out MovableRoadGeometry.Triangle[] triangles,out string? error)
    {
        triangles=Array.Empty<MovableRoadGeometry.Triangle>();error=null;
        try
        {
            var vertices = new MovableRoadGeometry.Vertex[positions.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var selector = selectors[i]; var p = positions[i]; var texture = uv[i];
                if (deformation != null)
                {
                    vertices[i] = deformation.Evaluate(p, normals[i],
                        tangents[i], texture, selector);
                    continue;
                }
                var distance = (p.z + selector.r) * (length * .5f);
                p = new Vector3((p.x - .5f) * width, p.y, distance - length * .5f);
                // Native vertex shader: nonnegative V is a repetition rate; negative V is
                // authored non-tiling UV, recovered with frac. UV.w is retained for material
                // conversion because the network fragment shader can consume it separately.
                texture.y = texture.y >= 0f ? distance * texture.y : texture.y - Mathf.Floor(texture.y);
                texture.z = 0f;
                vertices[i] = new MovableRoadGeometry.Vertex(p, normals[i],
                    tangents[i], texture, Color.white);
            }
            var result = new List<MovableRoadGeometry.Triangle>();
            var sourceTriangle = 0;
            for (var slot = 0; slot < indicesBySlot.Length; slot++)
            {
                var indices = indicesBySlot[slot];
                for (var i = 0; i < indices.Length; i += 3)
                    result.Add(new MovableRoadGeometry.Triangle(vertices[indices[i]], vertices[indices[i + 1]],
                        vertices[indices[i + 2]], slot, occurrence, sourceTriangle++));
            }
            triangles=result.ToArray();return true;
        }
        catch(Exception exception){error="Straight road evaluation failed: "+exception.Message;return false;}
    }

    private static bool Layout(Mesh mesh, VertexAttribute attribute, VertexAttributeFormat format, int dimension)
        => mesh.HasVertexAttribute(attribute) && mesh.GetVertexAttributeFormat(attribute) == format &&
            mesh.GetVertexAttributeDimension(attribute) == dimension;


}
