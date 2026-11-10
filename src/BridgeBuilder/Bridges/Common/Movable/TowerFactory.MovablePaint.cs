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
    // Original native decals follow the road through native relative child transforms.
    // Only projection volumes are partitioned; receiver topology is untouched.
    internal bool TryAttachMovablePaint(StaticObjectPrefab mechanism,
        IReadOnlyList<MovableLaneVisuals.Batch> batches, Pose roadToObject,
        MovableRoadGeometry.Partition partition, IReadOnlyList<MovableRoadGeometry.Triangle> receivers,out string? error)
        => TryAttachMovablePaint(mechanism, new[] { new MovablePaintDeck(batches, roadToObject, partition, receivers) },out error);

    internal sealed class PaintResult { internal bool Success; internal string? Error; }
    internal System.Threading.Tasks.Task<PaintResult> AttachMovablePaintAsync(StaticObjectPrefab mechanism,
        IReadOnlyList<MovableLaneVisuals.Batch> batches, Pose roadToObject,
        MovableRoadGeometry.Partition partition, IReadOnlyList<MovableRoadGeometry.Triangle> receivers) =>
        AttachMovablePaintAsync(mechanism,new[]{new MovablePaintDeck(batches,roadToObject,partition,receivers)});

    internal bool TryAttachMovablePaint(StaticObjectPrefab mechanism, IReadOnlyList<MovablePaintDeck> decks,out string? error)
    {
        var output=GenerationWork.Synchronous(()=>AttachMovablePaintAsync(mechanism,decks));
        error=output.Error;return output.Success;
    }
    internal async System.Threading.Tasks.Task<PaintResult> AttachMovablePaintAsync(StaticObjectPrefab mechanism, IReadOnlyList<MovablePaintDeck> decks)
    {
        var output=new PaintResult();bool cancelled=false;
        string? failure=null;bool success=false;
        var assets=new List<AssetData>();
        bool Fail(string cause){failure??=cause;return false;}
        try{success=await Build();output.Success=success;return output;}
        catch(OperationCanceledException){cancelled=true;throw;}
        catch(Exception exception){Fail("Native paint assembly: "+exception);return output;}
        finally
        {
            if(!success)
            {
                foreach(var asset in assets)try{asset.database.DeleteAsset(asset);}catch(Exception exception){failure=(failure??"Paint assembly failed")+"; cleanup: "+exception;}
                try{if(!cancelled)_report.Failed(mechanism.name+" [paint material]",new InvalidOperationException(failure));}
                catch(Exception exception){failure=(failure??"Paint assembly failed")+"; report: "+exception;}
            }
            output.Error=failure;
        }
        async System.Threading.Tasks.Task<bool> Build()
        {
        var root = (RenderPrefab)mechanism.m_Meshes[0].m_Mesh;
        var skeleton = root.GetComponent<ProceduralAnimationProperties>();
        if(skeleton==null || skeleton.m_Bones.Length==0)return Fail("Decal attachment requires the actual road skeleton.");
        var materials=new Dictionary<SurfaceAsset,SurfaceAsset>();
        var children=new List<ObjectSubObjectInfo>();
        int regionCount=0,faceCount=0;
        foreach(var deck in decks)
        foreach(var batch in deck.Batches.OrderBy(b=>b.Source.GetComponent<DecalProperties>()?.m_RendererPriority??0))
        {
            await GenerationWork.Checkpoint("native decal source");
            if(!batch.Decal || batch.Surfaces.Length!=1 || batch.Source.GetComponent<DecalProperties>()==null)
                return Fail("Native decal occurrence requires one source Surface and its projection properties.");
            var source=batch.Surfaces[0];
            if(MovableNativeDecals.IsSelectedRoadWear(batch))
            {
                Mod.Log.Info($"Selected-road wear excluded from mechanism: owner={mechanism.name}; source={source.id}; stationary ends included; prototype-owned wear retained.");
                continue;
            }
            if(!materials.TryGetValue(source,out var surface))
            {
                if(!MovableRigidDecalMaterial.TryCreate(source,mechanism.name+" Rigid Decal "+materials.Count,
                    _previewGeometry,assets,out surface,out var materialError)||materialError!=null)return Fail(materialError!);
                materials.Add(source,surface);
            }
            var properties=batch.Source.GetComponent<DecalProperties>();
            var regions=MovableNativeDecals.Regions(batch,deck.RoadToObject,32f).ToArray();
            var receivers=deck.Receivers;
            var visibleRegions=await GenerationWork.Background("decal receiver intersections",()=>regions.Where(region=>receivers.Any(region.Overlaps)).ToArray());
            foreach(var region in visibleRegions)
            {
                // Projection volumes are native DBuffer geometry, never opaque road
                // faces. Native shader depth reconstruction supplies the receiver.
                regionCount++;
                foreach(var part in MovableRigidDecalVolume.Build(region,deck.Partition))
                {
                    await GenerationWork.Checkpoint("native decal child publication");
                    var bone=skeleton.m_Bones[part.Bone];
                    int parent=bone.m_ConnectionID;
                    if(parent==0 && (bone.parentId!=-1 || bone.m_Type!=(BoneType)0))return Fail("A moving decal bone has no native connection ID.");
                    if(parent!=0 && (parent<mechanism.m_Meshes.Length || parent>=1000 || skeleton.m_Bones.Take(part.Bone).Any(b=>b.m_ConnectionID==parent)))
                        return Fail("A decal parent connection ID is ambiguous.");
                    string name=mechanism.name+" Native Decal "+children.Count;
                    if(!MovableRoadModel.TryBuild(name,part.Triangles,1,1,out var model,out var modelError))return Fail(modelError!);
                    using(model)
                    {
                        var render=ScriptableObject.CreateInstance<RenderPrefab>();_created.Add(render);
                        render.name=name+" Mesh";render.bounds=new Bounds3(-(float3)part.Size*.5f,(float3)part.Size*.5f);
                        render.vertexCount=model!.vertexCount;render.indexCount=model.indexCount;render.meshCount=1;
                        render.surfaceAssets=new[]{surface};render.surfaceArea=new Geometry(new[]{model}).CalcSurfaceArea();
                        var decal=render.AddComponent<DecalProperties>();
                        decal.m_TextureArea=new Bounds2(new float2(part.Area.x,part.Area.y),new float2(part.Area.z,part.Area.w));
                        decal.m_RendererPriority=properties.m_RendererPriority;
                        decal.m_EnableInfoviewColor=properties.m_EnableInfoviewColor;
                        decal.m_LayerMask=Game.Rendering.DecalLayers.Other;
                        if(_previewGeometry!=null)
                        {
                            render.hideFlags=HideFlags.HideAndDontSave;
                            if(!_previewGeometry.Capture(render,new[]{model},new[]{surface}))return Fail("Rigid decal preview capture failed.");
                        }
                        else
                        {
                            var asset=AssetDatabase.user.AddAsset(BridgeBuilder.Runtime.BridgePrefabPersistence.ResourcePath(render.name,"Geometry"),new Geometry(new[]{model}));
                            assets.Add(asset);render.geometryAsset=asset;asset.Save();asset.Unload();
                        }
                        var child=ScriptableObject.CreateInstance<StaticObjectPrefab>();_created.Add(child);
                        child.name=name;
                        child.m_Meshes=new[]{new ObjectMeshInfo {m_Mesh=render,m_Rotation=Quaternion.identity}};
                        // Native FindBoneIndex performs inverse-bind conversion. The
                        // saved child remains in the mechanism's closed object frame.
                        var mesh=mechanism.m_Meshes[0];
                        children.Add(new ObjectSubObjectInfo {m_Object=child,
                            m_Position=(Vector3)mesh.m_Position+(Quaternion)mesh.m_Rotation*part.Center,
                            m_Rotation=(Quaternion)mesh.m_Rotation*part.Rotation,
                            m_ParentMesh=parent,m_Probability=100});
                        faceCount+=part.Triangles.Length;
                    }
                }
            }
        }
        if(children.Count!=0)
        {
            var sub=mechanism.GetComponent<ObjectSubObjects>()??mechanism.AddComponent<ObjectSubObjects>();
            sub.m_SubObjects=(sub.m_SubObjects??Array.Empty<ObjectSubObjectInfo>()).Concat(children).ToArray();
            mechanism.isDirty=true;
        }
        Mod.Log.Info($"Native rigid decals: owner={mechanism.name}; sources={materials.Count}; projectionRegions={regionCount}; children={children.Count}; volumeTriangles={faceCount}; original textures/mips; native continuous alpha; native bone connections.");
        return true;
        }
    }

}
