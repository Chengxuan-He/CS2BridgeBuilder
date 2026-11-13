using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Systems;

internal static class BridgeMeshRetirement
{
    private const BindingFlags Fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    private static FieldInfo Member(Type type,string name) => type.GetField(name,Fields)
        ?? throw new MissingFieldException(type.FullName,name);
    private static object? Read(object item,string name)=>Member(item.GetType(),name).GetValue(item);
    private static bool Uses(EntityManager manager, Entity mesh, HashSet<Entity> owned)
    {
        if(owned.Contains(mesh)) return true;
        if(!manager.HasBuffer<NetCompositionPiece>(mesh)) return false;
        var pieces=manager.GetBuffer<NetCompositionPiece>(mesh,true);
        for(int i=0;i<pieces.Length;i++) if(owned.Contains(pieces[i].m_Piece)) return true;
        return false;
    }

    internal static string Advance(World world,BatchMeshSystem meshes,HashSet<Entity> owned,
        IDictionary infos,IDictionary caching,string owner,HashSet<UnityEngine.Object> destroying,out bool inUse)
    {
        inUse=true;
        var batches=world.GetExistingSystemManaged<BatchManagerSystem>();
        if(batches==null)return "no native batch manager";
        var manager=world.EntityManager;
        var states=meshes.GetLoadingState(out var stateJobs);
        var groups=batches.GetNativeBatchGroups(true,out var groupJobs);
        var instances=batches.GetNativeBatchInstances(true,out var instanceJobs);
        stateJobs.Complete();groupJobs.Complete();instanceJobs.Complete();
        var managed=batches.GetManagedBatches();
        var empty=new HashSet<int>();
        var protectedBatches=new HashSet<int>();
        var merged=new HashSet<int>();
        int groupCount=groups.GetGroupCount();
        for(int g=0;g<groupCount;g++)
        {
            if(!groups.IsValidGroup(g))continue;
            for(int j=0;j<groups.GetMergedGroupCount(g);j++)
            {
                merged.Add(g);
                merged.Add(groups.GetMergedGroupIndex(g,j));
            }
        }
        for(int group=0;group<groupCount;group++)
        {
            if(!groups.IsValidGroup(group))continue;
            var bucket=instances.GetInstanceCount(group)==0 && !merged.Contains(group) ? empty : protectedBatches;
            for(int j=0;j<groups.GetBatchCount(group);j++)
            {
                int slot=groups.GetManagedBatchIndex(group,j);
                if(slot>=0)bucket.Add(slot);
            }
        }
        inUse=false;
        var referenced=new HashSet<Entity>();
        var eligible=new HashSet<int>();
        int direct=0, visible=0, borrowed=0;
        void Record(Entity mesh)
        {
            if(owned.Contains(mesh))referenced.Add(mesh);
            if(!manager.HasBuffer<NetCompositionPiece>(mesh))return;
            var pieces=manager.GetBuffer<NetCompositionPiece>(mesh,true);
            for(int j=0;j<pieces.Length;j++)if(owned.Contains(pieces[j].m_Piece))referenced.Add(pieces[j].m_Piece);
        }
        for(int i=0;i<managed.BatchCount;i++)
        {
            if(!(managed.GetBatch(i) is CustomBatch batch))continue;
            Record(batch.sourceMeshEntity);Record(batch.sharedMeshEntity);
            if(!Uses(manager,batch.sharedMeshEntity,owned))continue;
            direct++;
            if(protectedBatches.Contains(i)){visible++;inUse=true;continue;}
            if(!empty.Contains(i)){inUse=true;continue;}
            if(i<states.Length && (states[i]==MeshLoadingState.None || states[i]==MeshLoadingState.Default))continue;
            if(i>=states.Length || (states[i]!=MeshLoadingState.Complete && states[i]!=MeshLoadingState.Obsolete &&
                states[i]!=MeshLoadingState.Pending && states[i]!=MeshLoadingState.Loading)){inUse=true;continue;}
            // Keep an independently registered source that still uses the owner's shared rendering mesh.
            if(batch.sourceMeshEntity!=batch.sharedMeshEntity && !owned.Contains(batch.sourceMeshEntity)){borrowed++;inUse=true;continue;}
            eligible.Add(i);
        }
        if(eligible.Count!=0)
        {
            foreach(int index in eligible)
                ReleaseBatch(meshes,batches,manager,infos,(CustomBatch)managed.GetBatch(index),index,destroying);
            Mod.Log.Info($"Released {eligible.Count} exclusive instance-free native batch slots for {owner}.");
        }
        // An inert piece can outlive RemoveBatch's destruction of its parent composition.
        // Even a dormant managed batch, or another loaded composition, bars orphan reclamation.
        foreach(Entity mesh in infos.Keys) if(manager.HasBuffer<NetCompositionPiece>(mesh)) Record(mesh);
        foreach(var mesh in owned)
        {
            if(manager.Exists(mesh)||referenced.Contains(mesh)||caching.Contains(mesh)||!(infos[mesh] is object info))continue;
            if(!(Read(info,"m_Prefab") is RenderPrefab prefab) || prefab.geometryAsset==null)continue;
            if(DropUnclaimedRecord(meshes,infos,mesh,info,prefab.geometryAsset))
                Mod.Log.Info($"Released orphaned native mesh cache {mesh} ({prefab.name}) for {owner}.");
        }
        return $"matchingBatches={direct}, occupied={visible}, releasedSlots={eligible.Count}, foreignShared={borrowed}";
    }

    private static bool DropUnclaimedRecord(BatchMeshSystem meshes, IDictionary infos, Entity mesh,object info, GeometryAsset geometry)
    {
        if((int)Read(info,"m_GeneratedIndex")! != -1 || Read(info,"m_ShapeAllocations")!=null ||
            Read(info,"m_OverlayAllocations")!=null || Read(info,"m_CullAllocations")!=null ||
            (int)Member(typeof(GeometryAsset),"m_InstanceRefCount").GetValue(geometry)! != 0 ||
            Member(typeof(GeometryAsset),"m_Instances").GetValue(geometry)!=null ||
            geometry.loading.m_AsyncLoadingStarted || geometry.loading.m_AsyncLoadingScheduled) return false;
        var bytes=(ulong)Read(info,"m_SizeInMemory")!;
        // The entity, and hence its ECS mesh buffers, are already destroyed. No generated mesh
        // or heap block exists. Finish the two remaining native piece-unload operations.
        typeof(BatchMeshSystem).GetProperty(nameof(BatchMeshSystem.totalSizeInMemory))!.SetValue(meshes,meshes.totalSizeInMemory-bytes);
        infos.Remove(mesh);
        return true;
    }

    // Use the native batch removal path for its heap accounting, queues and geometry handles.
    // The group still owns its slot, so retain an empty loading-state/priority slot at the same index.
    private static void ReleaseBatch(BatchMeshSystem meshes,BatchManagerSystem batches,EntityManager manager,
        IDictionary infos,CustomBatch batch,int index,HashSet<UnityEngine.Object> destroying)
    {
        var states=meshes.GetLoadingState(out var stateFence);
        var priority=meshes.GetBatchPriority(out var priorityFence);
        var groups=batches.GetNativeBatchGroups(false,out var groupFence);
        stateFence.Complete();priorityFence.Complete();groupFence.Complete();
        var resident=states[index]==MeshLoadingState.Complete || states[index]==MeshLoadingState.Obsolete;
        var stateLength=states.Length;
        var priorityLength=priority.Length;
        var pieces=new List<Entity>();
        if(resident)
        {
            if(infos[batch.sharedMeshEntity] is object current && (int)Read(current,"m_BatchCount")! <= 1 &&
                manager.HasBuffer<NetCompositionPiece>(batch.sharedMeshEntity))
            {
                var parts=manager.GetBuffer<NetCompositionPiece>(batch.sharedMeshEntity,true);
                for(int j=0;j<parts.Length;j++) pieces.Add(parts[j].m_Piece);
            }
            if(infos[batch.sharedMeshEntity] is object entry && (int)Read(entry,"m_BatchCount")!<=1 &&
                (int)Read(entry,"m_GeneratedIndex")! is int generated && generated>=0 &&
                ((IList)Read(meshes,"m_GeneratedMeshes")!)[generated] is UnityEngine.Mesh mesh)
                destroying.Add(mesh);
            var managed=batches.GetManagedBatches();
            managed.SetMesh(index,meshes.GetDefaultMesh(batch.sourceType,batch.sourceFlags,batch.generatedType),0,groups);
            if(batch.defaultMaterial!=batch.loadedMaterial)managed.SetMaterial(index,batch.defaultMaterial,groups);
        }
        meshes.RemoveBatch(batch,index);
        if(states.Length<stateLength)states.Add(MeshLoadingState.None);
        if(priority.Length<priorityLength)priority.Add(-1000000);
        // RemoveBatch handles the batch root. Its composition children use separate residency
        // records; mirror the game's per-piece release when the final root reference disappears.
        foreach(var piece in pieces)
        {
            if(!(infos[piece] is object child))continue;
            var count=(int)Read(child,"m_BatchCount")!;
            if(count>1){Member(child.GetType(),"m_BatchCount").SetValue(child,count-1);infos[piece]=child;continue;}
            var size=(ulong)Read(child,"m_SizeInMemory")!;
            typeof(BatchMeshSystem).GetMethod("UncacheMeshData",Fields)!.Invoke(meshes,new object[]{piece,batch.sourceType});
            typeof(BatchMeshSystem).GetProperty(nameof(BatchMeshSystem.totalSizeInMemory))!.SetValue(meshes,meshes.totalSizeInMemory-size);
            infos.Remove(piece);
        }
    }
}
