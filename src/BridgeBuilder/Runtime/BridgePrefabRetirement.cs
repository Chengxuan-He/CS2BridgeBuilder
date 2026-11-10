using System;
using System.Collections;
using System.Reflection;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Entities;

namespace BridgeBuilder.Runtime;

// File-loading completion precedes the native Created/Updated prefab passes.
internal static class BridgePrefabRetirement
{
    private static readonly FieldInfo Updates = typeof(PrefabSystem).GetField("m_UpdateMap",
        BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(PrefabSystem).FullName,"m_UpdateMap");

    internal static bool ContextReady
    {
        get
        {
            var game=GameManager.instance;
            return game != null && game.state==GameManager.State.WorldReady && !game.isGameLoading
                && (game.gameMode==GameMode.MainMenu || (game.gameMode & (GameMode.Game|GameMode.Editor))!=0)
                && BridgeAssetLoading.Ready && !BridgeDeferredRemoval.Saving
                && !BridgeBuilder.Systems.BridgeStartupAssetSystem.IsStartupInspection;
        }
    }

    internal static bool Ready(EntityManager manager,PrefabSystem prefabs)
    {
        if(!ContextReady) return false;
        if(Updates.GetValue(prefabs) is not IDictionary updates)
            throw new InvalidOperationException("Native prefab replacement queue is unavailable.");
        if(updates.Count!=0) return false;
        using var pending=manager.CreateEntityQuery(new EntityQueryDesc {
            All=new[]{ComponentType.ReadOnly<PrefabData>()},
            Any=new[]{ComponentType.ReadOnly<Created>(),ComponentType.ReadOnly<Updated>()}
        });
        return pending.IsEmptyIgnoreFilter;
    }
}
