using Game.Modding;
using Game.Prefabs;
using Game.SceneFlow;
using System;
using System.Linq;

namespace BridgeBuilder.Bridges;

/// <summary>Optional integration only; never loads or references RoadBuilder.dll.</summary>
internal static class RoadBuilderCompatibility
{
    internal static bool IsAvailable
    {
        get
        {
            try
            {
                // An assembly may remain loaded after disposal or a failed OnLoad. The game's
                // successful mod state is required, not files in the cache or a prefab's presence.
                var mods = GameManager.instance?.modManager;
                return mods != null && mods.Any(mod => mod.state == ModManager.ModInfo.State.Loaded
                    && string.Equals(mod.asset?.assembly?.GetName().Name, "RoadBuilder", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal static bool OwnsPrefab(PrefabBase prefab)
    {
        // Check ownership, never the user-editable asset name. Include derived track/path types
        // before DeckCatalog's ordinary-network branches can admit them.
        for (var type = prefab.GetType(); type != null; type = type.BaseType)
            if (string.Equals(type.Assembly.GetName().Name, "RoadBuilder", StringComparison.OrdinalIgnoreCase)
                || (type.Namespace?.StartsWith("RoadBuilder.", StringComparison.Ordinal) ?? false))
                return true;
        return false;
    }
}
