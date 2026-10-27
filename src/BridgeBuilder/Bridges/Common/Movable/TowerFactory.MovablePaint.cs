using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    // Marking surfaces use the original road skeleton on the same native object.
    // Only paint polygons are partitioned; receiver topology is untouched.
    internal void AttachMovablePaint(StaticObjectPrefab mechanism,
        IReadOnlyList<MovableLaneVisuals.Batch> batches, Pose roadToObject,
        MovableRoadGeometry.Partition partition, IReadOnlyList<MovableRoadGeometry.Triangle> receivers)
        => AttachMovablePaint(mechanism, new[] { new MovablePaintDeck(batches, roadToObject, partition, receivers) });

    internal void AttachMovablePaint(StaticObjectPrefab mechanism, IReadOnlyList<MovablePaintDeck> decks)
    {
        var triangles = new List<MovableRoadGeometry.BoundTriangle>();
        foreach (var deck in decks)
        {
            var batches = deck.Batches;
            var roadToObject = deck.RoadToObject;
            var partition = deck.Partition;
            var receivers = deck.Receivers;
            var groups = new Dictionary<(int Bone, bool Yellow, int Priority, bool Infoview, Quaternion Rotation), List<Vector3[]>>();
            foreach (var batch in batches)
            {
                var properties = batch.Source.GetComponent<DecalProperties>();
                foreach (var region in MovableNativeDecals.Regions(batch, roadToObject, 32f))
                {
                    var corners = new List<Vector3> {
                        region.Center-region.Right*region.Half.x-region.Forward*region.Half.z,
                        region.Center-region.Right*region.Half.x+region.Forward*region.Half.z,
                        region.Center+region.Right*region.Half.x+region.Forward*region.Half.z,
                        region.Center+region.Right*region.Half.x-region.Forward*region.Half.z };
                    Divide(corners, partition);
                    void Divide(List<Vector3> polygon, MovableRoadGeometry.Partition part)
                    {
                        if (polygon.Count < 3 || part.IsGap) return;
                        if (part.Bone >= 0)
                        {
                            var key = (part.Bone, region.Yellow, properties.m_RendererPriority, properties.m_EnableInfoviewColor, Quaternion.LookRotation(region.Forward,region.Up));
                            if (!groups.TryGetValue(key, out var volumes)) groups.Add(key, volumes = new());
                            var volume = new Vector3[polygon.Count];
                            for(int i=0;i<polygon.Count;i++)
                            {
                                // User-approved 1 mm physical paint clearance, following
                                // the same skin transform as the original road.
                                volume[i]=RoadHeight(polygon[i])+region.Up*.001f;
                            }
                            volumes.Add(volume);
                            return;
                        }
                        var negative=new List<Vector3>();var positive=new List<Vector3>();
                        var previous=polygon[polygon.Count-1];
                        var before=part.ObjectToSeam.MultiplyPoint3x4(previous).z;
                        foreach(var point in polygon)
                        {
                            var distance=part.ObjectToSeam.MultiplyPoint3x4(point).z;
                            if((before<0 && distance>0)||(before>0 && distance<0))
                            {
                                var crossing=Vector3.LerpUnclamped(previous,point,before/(before-distance));
                                negative.Add(crossing);positive.Add(crossing);
                            }
                            if(distance<=0)negative.Add(point);
                            if(distance>=0)positive.Add(point);
                            previous=point;before=distance;
                        }
                        Divide(negative,part.Negative!);Divide(positive,part.Positive!);
                    }
                }
            }
            Vector3 RoadHeight(Vector3 point)
            {
                var found=false;var best=float.PositiveInfinity;var height=0f;
                foreach(var triangle in receivers)
                {
                    if(triangle.A.Normal.y+triangle.B.Normal.y+triangle.C.Normal.y<1.5f)continue;
                    var a=triangle.A.Position;var ab=triangle.B.Position-a;var ac=triangle.C.Position-a;
                    var d=point-a;var determinant=ab.x*ac.z-ab.z*ac.x;
                    if(Mathf.Abs(determinant)<1e-10f)continue;
                    var u=(d.x*ac.z-d.z*ac.x)/determinant;
                    var v=(ab.x*d.z-ab.z*d.x)/determinant;
                    if(u<-.0001f || v<-.0001f || u+v>1.0001f)continue;
                    var y=a.y+u*ab.y+v*ac.y;var distance=Mathf.Abs(y-point.y);
                    if(distance>=best)continue;
                    found=true;best=distance;height=y;
                }
                if(!found)throw new InvalidOperationException($"Native paint has no upward road receiver at {point}.");
                return new Vector3(point.x,height,point.z);
            }
            foreach (var group in groups)
            foreach (var polygon in group.Value)
            {
                var uv = group.Key.Yellow ? new Vector4(400.5f/2048f,35.5f/2048f,0f,0f)
                    : new Vector4(14.5f/2048f,1610.5f/2048f,0f,0f);
                var normal = group.Key.Rotation * Vector3.up;
                var right = group.Key.Rotation * Vector3.right;
                MovableRoadGeometry.Vertex Vertex(int i) => new(polygon[i], normal,
                    new Vector4(right.x,right.y,right.z,1f), uv, Color.white);
                for (var i=1;i<polygon.Length-1;i++)
                    triangles.Add(new MovableRoadGeometry.BoundTriangle(
                        new MovableRoadGeometry.Triangle(Vertex(0),Vertex(i),Vertex(i+1),0,-1,-1),(uint)group.Key.Bone));
            }
        }
        if(triangles.Count==0)return;
        var root = (RenderPrefab)mechanism.m_Meshes[0].m_Mesh;
        var skeleton = root.GetComponent<ProceduralAnimationProperties>();
        var assets = new List<AssetData>();
        try
        {
            var surface = BridgeBuilder.Runtime.BridgePreparedMaterials.Paint(
                _styleId ?? throw new InvalidOperationException("Movable paint requires a bridge style."));
            if(!MovableRoadModel.TryBuild(mechanism.name+" Skinned Paint",triangles,1,
                skeleton.m_Bones.Length,out var model,out var error))throw new InvalidOperationException(error);
            using(model)
            {
                var render=ScriptableObject.CreateInstance<RenderPrefab>(); _created.Add(render);
                render.name=mechanism.name+" Skinned Paint Mesh";
                // A single low-poly paint mesh spans all road LODs. No individual dash
                // is distance-culled as a tiny independent child projector anymore.
                render.bounds=root.bounds;
                render.vertexCount=model!.vertexCount;render.indexCount=model.indexCount;render.meshCount=1;
                render.surfaceAssets=new[]{surface};render.surfaceArea=new Geometry(new[]{model}).CalcSurfaceArea();
                render.AddComponentFrom(skeleton);
                // The mechanism is a native stacked render. Skinning alone is not
                // enough: paint must receive the identical stack placement/scale.
                // Equal authored bounds union into the same native stack range.
                var stack = root.GetComponent<StackProperties>();
                if (stack != null) render.AddComponentFrom(stack);
                if(_previewGeometry!=null)
                {
                    render.hideFlags=HideFlags.HideAndDontSave;
                    if(!_previewGeometry.Capture(render,new[]{model},new[]{surface}))
                        throw new InvalidOperationException("Skinned paint preview capture failed.");
                }
                else
                {
                    var asset=AssetDatabase.user.AddAsset(BridgeBuilder.Runtime.BridgePrefabPersistence.ResourcePath(render.name, "Geometry"),new Geometry(new[]{model}));
                    assets.Add(asset);render.geometryAsset=asset;asset.Save();asset.Unload();
                }
                // Preserve original mesh ordering and every existing connection code.
                // Native animation updates this surface in the same object/frame as
                // the road, using identical bone definitions, indices and bind poses.
                mechanism.m_Meshes=mechanism.m_Meshes.Concat(new[]{new ObjectMeshInfo {
                    m_Mesh=render,m_Position=mechanism.m_Meshes[0].m_Position,
                    m_Rotation=mechanism.m_Meshes[0].m_Rotation }}).ToArray();
            }
            Mod.Log.Info($"Native skinned paint: triangles={triangles.Count}; clearance=0.001m; native object meshes=1; child projectors=0; road cuts=0.");
        }
        catch
        {
            foreach(var asset in assets)asset.database.DeleteAsset(asset);
            throw;
        }
    }

}
