using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static class MovableBoneDecals
{
    internal sealed class Result
    {
        internal bool Success;
        internal string? Error;
        internal ModelImporter.Model[] Models = Array.Empty<ModelImporter.Model>();
        internal SurfaceAsset[] Surfaces = Array.Empty<SurfaceAsset>();
        internal Bounds Bounds;
    }

    internal static async Task<Result> BuildAsync(string owner, IReadOnlyList<MovablePaintDeck> decks,
        int boneCount, PreviewGeometry? preview, List<AssetData> assets, List<ModelImporter.Model> owned)
    {
        var result = new Result();
        try
        {
            var groups = new Dictionary<(SurfaceAsset Source, int Priority, float DistanceFactor), List<MovableRigidDecalVolume.Part>>();
            var calculations = new List<Func<MovableRigidDecalVolume.Part[]>>();
            var keys = new List<(SurfaceAsset Source, int Priority, float DistanceFactor)>();
            foreach (var deck in decks)
            foreach (var batch in deck.Batches.OrderBy(batch => batch.Source.GetComponent<DecalProperties>()?.m_RendererPriority ?? 0))
            {
                await GenerationWork.Checkpoint("current-bone decal source");
                var properties = batch.Source.GetComponent<DecalProperties>();
                if (MovableNativeDecals.IsLaneColor(batch) || MovableNativeDecals.IsSelectedRoadWear(batch))
                {
                    Mod.Log.Info($"Selected-road decal omitted by mechanism appearance policy: owner={owner}; source={batch.Surfaces[0].id}; ordinary asphalt and prototype-owned wear.");
                    continue;
                }
                var regions = MovableNativeDecals.Regions(batch, deck.RoadToObject, 32f).ToArray();
                var receivers = deck.Receivers;
                var partition = deck.Partition;
                calculations.Add(() => regions.Where(region => receivers.Any(region.Overlaps))
                    .SelectMany(region => MovableRigidDecalVolume.Build(region, partition)).ToArray());
                keys.Add((batch.Surfaces[0], properties.m_RendererPriority, batch.DecalDistanceFactor));
            }
            var batches = await GenerationWork.Background("current-bone decal volumes", () =>
                calculations.Select(calculate => calculate()).ToArray());
            for (int i = 0; i < batches.Length; i++)
            {
                var parts = batches[i];
                if (parts.Length == 0) continue;
                var key = keys[i];
                if (!groups.TryGetValue(key, out var group)) groups.Add(key, group = new List<MovableRigidDecalVolume.Part>());
                group.AddRange(parts);
            }
            var models = new List<ModelImporter.Model>(); var surfaces = new List<SurfaceAsset>();
            bool haveBounds = false; int volumeCount = 0;
            foreach (var group in groups.OrderBy(group => group.Key.Priority))
            {
                await GenerationWork.Checkpoint("current-bone decal native model");
                if (!MovableRigidDecalMaterial.TryCreate(group.Key.Source, owner + " Bone Decal " + models.Count,
                    preview, assets, out var surface, out var error, rendererPriority: group.Key.Priority,
                    distanceFactor: group.Key.DistanceFactor))
                { result.Error = error; return result; }
                if (!MovableBoneDecalModel.TryBuild(owner + " Bone Decal " + models.Count, group.Value, boneCount, out var model, out error))
                { result.Error = error; return result; }
                owned.Add(model!); models.Add(model!); surfaces.Add(surface); volumeCount += group.Value.Count;
                foreach (var part in group.Value)
                foreach (var triangle in part.Triangles)
                {
                    Include(triangle.Geometry.A.Position); Include(triangle.Geometry.B.Position); Include(triangle.Geometry.C.Position);
                    void Include(Vector3 local)
                    {
                        var point = part.Center + part.Rotation * local;
                        if (!haveBounds) { result.Bounds = new Bounds(point, Vector3.zero); haveBounds = true; }
                        else result.Bounds.Encapsulate(point);
                    }
                }
            }
            result.Models = models.ToArray(); result.Surfaces = surfaces.ToArray(); result.Success = true;
            Mod.Log.Info($"Current-bone decals: owner={owner}; surfaces={models.Count}; projectionVolumes={volumeCount}; shared mechanism models; source art/alpha; no relative children.");
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { result.Error = "Current-bone decal assembly: " + exception; return result; }
    }
}
