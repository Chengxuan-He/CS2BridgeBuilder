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
        int[][]? sourceVertexIndices = null, int[][]? recordedCornerIndices = null, Type? renderType = null, SurfaceAsset[]? receivingSurfaces = null) where T : RenderPrefab
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
            var surfaces = receivingSurfaces ?? source.surfaceAssets?.ToArray() ?? Array.Empty<SurfaceAsset>();
            var bounds = new Bounds3(new float3(float.PositiveInfinity), new float3(float.NegativeInfinity));
            var vertices = 0;
            var indices = 0;
            for (var i = 0; i < meshes.Length; i++)
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
                    if (sourceFrame.HasValue || xScaleCoefficients != null || xShearYCoefficients != null || xShearZCoefficients != null)
                    {
                        for (var vertex = 0; vertex < points.Length; vertex++)
                        {
                            var coefficient = xScaleCoefficients?[i][vertex] ?? 0f;
                            var shearY = xShearYCoefficients?[i][vertex] ?? 0f;
                            var shearZ = xShearZCoefficients?[i][vertex] ?? 0f;
                            if (!sourceFrame.HasValue && ((coefficient == 0f && shearY == 0f && shearZ == 0f) || halfDelta == 0f)) continue;
                            var frame = Matrix4x4.Scale(new Vector3(1f + halfDelta * coefficient, 1f, 1f));
                            frame.m01 = halfDelta * shearY;
                            frame.m02 = halfDelta * shearZ;
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
                    sourceVertexIndices: sourceVertexIndices?[i], recordedCornerIndices: recordedCornerIndices?[i])
                    ?? throw new InvalidOperationException(error);
                models.Add(model);
                vertices += points.Length;
                indices += omittedTriangles == null ? checked((int)CountIndices(meshes[i]))
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
