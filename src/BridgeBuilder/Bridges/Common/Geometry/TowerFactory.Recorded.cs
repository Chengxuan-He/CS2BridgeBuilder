using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;
using BridgeBuilder.Runtime;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    // Inputs are temporary native descriptors reconstructed from offline measurements.
    // Every internal mesh supplies an immutable, vertex-ordered displacement table.
    internal T CreateRecordedRender<T>(T source, string name, float halfDelta,
        float[][] xCoefficients, float[][]? xScaleCoefficients = null,
        Bounds3? placementBounds = null, float[][]? xShearYCoefficients = null,
        float[][]? xShearZCoefficients = null) where T : RenderPrefab
        => GenerationWork.Synchronous(() => CreateRecordedRenderAsync(source, name, halfDelta,
            xCoefficients, xScaleCoefficients, placementBounds, xShearYCoefficients, xShearZCoefficients));

    internal async System.Threading.Tasks.Task<T> CreateRecordedRenderAsync<T>(T source, string name, float halfDelta,
        float[][] xCoefficients, float[][]? xScaleCoefficients = null,
        Bounds3? placementBounds = null, float[][]? xShearYCoefficients = null,
        float[][]? xShearZCoefficients = null, float3[][]? recordedOrigins = null,
        Matrix4x4? sourceFrame = null, bool[][]? omittedTriangles = null,
        int[][]? sourceVertexIndices = null, int[][]? recordedCornerIndices = null, Type? renderType = null, SurfaceAsset[]? receivingSurfaces = null,
        int[]? sourceMeshIndices = null, int[][]? sourceTriangleVertices = null,
        float[][]? sourceTriangleWeights = null, int[][]? explicitTriangleIndices = null,
        int[][]? sourceFaceIndices = null, Vector2[][]? recordedUv0 = null,
        Matrix4x4[][]? recordedFrameTransforms = null) where T : RenderPrefab
    {
        name = TowerPrefabNaming.Safe(name);
        var meshes = PrivateGeometryReader.Read(source)
            ?? throw new InvalidOperationException("Recorded geometry is unavailable: " + source.name);
        var models = new List<ModelImporter.Model>();
        var transferred = false;
        var result = (T)ScriptableObject.CreateInstance(renderType ?? typeof(T));
        _created.Add(result);
        result.name = name;
        try
        {
            var sourceSurfaces = receivingSurfaces ?? source.surfaceAssets?.ToArray() ?? Array.Empty<SurfaceAsset>();
            // Measured roles can omit entire internal meshes. Keep source-indexed recipe
            // arrays while emitting their matching surfaces in the selected model order.
            var selectedMeshes = sourceMeshIndices ?? Enumerable.Range(0, meshes.Length).ToArray();
            var surfaces = sourceMeshIndices == null ? sourceSurfaces : selectedMeshes.Select(i => sourceSurfaces[i]).ToArray();
            var bounds = new Bounds3(new float3(float.PositiveInfinity), new float3(float.NegativeInfinity));
            var vertices = 0;
            var indices = 0;
            foreach (var i in selectedMeshes)
            {
                var points = recordedOrigins == null ? ToPoints(meshes[i].vertices) : recordedOrigins[i].ToArray();
                var prepared = await GenerationWork.Background("recorded structure coordinates and frames", () =>
                {
                    var localBounds = new Bounds3(new float3(float.PositiveInfinity), new float3(float.NegativeInfinity));
                    AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.DenseDelta,
                        new[] { points, points }, new[] { xCoefficients[i] }, new[] { halfDelta }, count: points.Length);
                    foreach (var point in points)
                    {
                        localBounds.min = math.min(localBounds.min, point);
                        localBounds.max = math.max(localBounds.max, point);
                    }
                    var frames = new List<KeyValuePair<int, Matrix4x4>>();
                    if (sourceFrame.HasValue || recordedFrameTransforms != null || xScaleCoefficients != null || xShearYCoefficients != null || xShearZCoefficients != null)
                    {
                        for (var vertex = 0; vertex < points.Length; vertex++)
                        {
                            var coefficient = xScaleCoefficients?[i][vertex] ?? 0f;
                            var shearY = xShearYCoefficients?[i][vertex] ?? 0f;
                            var shearZ = xShearZCoefficients?[i][vertex] ?? 0f;
                            if (!sourceFrame.HasValue && recordedFrameTransforms == null && ((coefficient == 0f && shearY == 0f && shearZ == 0f) || halfDelta == 0f)) continue;
                            var frame = Matrix4x4.Scale(new Vector3(1f + halfDelta * coefficient, 1f, 1f));
                            frame.m01 = halfDelta * shearY;
                            frame.m02 = halfDelta * shearZ;
                            // The recorded Jacobian acts in construction coordinates, after
                            // source-frame mapping and before the ordinary width Jacobian.
                            if (recordedFrameTransforms != null) frame *= recordedFrameTransforms[i][vertex];
                            if (sourceFrame.HasValue) frame *= sourceFrame.Value;
                            frames.Add(new KeyValuePair<int, Matrix4x4>(vertex, frame));
                        }
                    }
                    return (Bounds: localBounds, Frames: frames.ToArray());
                });
                bounds.min = math.min(bounds.min, prepared.Bounds.min);
                bounds.max = math.max(bounds.max, prepared.Bounds.max);
                var model = BuildModel(name + " Mesh " + i, meshes[i], points, out var error,
                    frameTransforms: prepared.Frames, omittedTriangles: omittedTriangles?[i],
                    sourceVertexIndices: sourceVertexIndices?[i], recordedCornerIndices: recordedCornerIndices?[i],
                    sourceTriangleVertices: sourceTriangleVertices?[i], sourceTriangleWeights: sourceTriangleWeights?[i],
                    explicitTriangleIndices: explicitTriangleIndices?[i], sourceFaceIndices: sourceFaceIndices?[i], recordedUv0: recordedUv0?[i])
                    ?? throw new InvalidOperationException(error);
                models.Add(model);
                vertices += points.Length;
                indices += explicitTriangleIndices != null ? explicitTriangleIndices[i].Length : omittedTriangles == null ? checked((int)CountIndices(meshes[i]))
                    : checked(omittedTriangles[i].Count(omit => !omit) * 3);
            }
            if (_previewGeometry != null)
            {
                if (!_previewGeometry.Capture(result, models, surfaces)) throw new InvalidOperationException("Recorded preview geometry capture failed.");
                result.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                if (!Runtime.BridgeAssetInfo.MatchesOwner(name, _bridgeName))
                    throw new InvalidOperationException("Recorded geometry must retain its owner identity.");
                var asset = AssetDatabase.user.AddAsset(Runtime.BridgePrefabPersistence.ResourcePath(name, "Geometry"), new Geometry(models.ToArray()));
                asset.Save();
                asset.Unload();
                result.geometryAsset = asset;
                transferred = true;
            }
            foreach (var component in source.components)
                if (component != null && component is not LodProperties) result.AddComponentFrom(component);
            // External LODs are assembled explicitly by the measured recipe, never borrowed.
            // Indexed mesh bounds were computed by BuildModel. Authored placement/stacking
            // bounds may deliberately extend below the visible geometry.
            result.bounds = placementBounds ?? bounds;
            result.vertexCount = vertices;
            result.indexCount = indices;
            result.meshCount = models.Count;
            result.surfaceArea = source.surfaceArea;
            result.surfaceAssets = surfaces;
            result.isImpostor = source.isImpostor;
            result.manualVTRequired = source.manualVTRequired;
            return result;
        }
        finally
        {
            if (!transferred) foreach (var model in models) model.Dispose();
            PrivateGeometryReader.Release(meshes);
        }
    }
}
