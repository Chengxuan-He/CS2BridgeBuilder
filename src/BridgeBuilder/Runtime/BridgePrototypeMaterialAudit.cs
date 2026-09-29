using BridgeBuilder.Bridges;
using Colossal.IO.AssetDatabase;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBuilder.Runtime;

/// <summary>Read-only coverage of installed donors, their structure replacements and all LODs.</summary>
internal sealed class BridgePrototypeMaterialAudit
{
    private readonly HashSet<PrefabBase> _checked = new();

    internal void Inspect(PrefabSystem system)
    {
        var roots = BridgeStyleCatalog.Styles.SelectMany(style => style.Variants)
            .Select(variant => variant.Donor).Distinct().Where(root => !_checked.Contains(root)).ToArray();
        if (roots.Length == 0) return;
        var replacements = new Dictionary<PrefabBase, List<PrefabBase>>();
        foreach (var candidate in PrefabCatalog.GetAll(system).OfType<ObjectGeometryPrefab>())
        {
            if (!candidate.TryGet<SpawnableObject>(out var spawn) || spawn.m_Placeholders == null) continue;
            foreach (var placeholder in spawn.m_Placeholders)
            {
                if (placeholder == null) continue;
                if (!replacements.TryGetValue(placeholder, out var list))
                    replacements.Add(placeholder, list = new List<PrefabBase>());
                list.Add(candidate);
            }
        }
        var pending = new Queue<PrefabBase>(roots);
        var visited = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
        var surfaces = new HashSet<SurfaceAsset>();
        var renders = 0;
        var missing = 0;
        void Visit(object? value)
        {
            if (value == null || value is string) return;
            if (value is PrefabBase prefab)
            {
                if (prefab is NetGeometryPrefab || prefab is NetSectionPrefab ||
                    prefab is RenderPrefab || prefab is ObjectGeometryPrefab) pending.Enqueue(prefab);
                return;
            }
            if (value is UnityEngine.Object || value is AssetData) return;
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || !visited.Add(value)) return;
            if (value is IEnumerable items) { foreach (var item in items) Visit(item); return; }
            if ((type.Namespace ?? string.Empty).StartsWith("Game.", StringComparison.Ordinal)) Fields(value);
        }
        void Fields(object value)
        {
            foreach (var field in SerializedFields.Of(value.GetType()))
                if (field.Name != nameof(PrefabBase.components)) Visit(field.GetValue(value));
        }
        while (pending.Count != 0)
        {
            var prefab = pending.Dequeue();
            if (!_checked.Add(prefab)) continue;
            try
            {
                if (prefab is RenderPrefab render)
                {
                    renders++;
                    for (var slot = 0; slot < render.materialCount; slot++)
                    {
                        var surface = render.GetSurfaceAsset(slot);
                        if (surface == null)
                        {
                            missing++;
                            Mod.Log.Warn($"Prototype material audit: '{render.name}' has unresolved surface slot {slot}.");
                        }
                        else if (surfaces.Add(surface))
                        {
                            // Do not Load/Unload the renderer's shared material, textures or VT state.
                            // VT texture dummies legitimately have no independent texture file.
                            try
                            {
                                using var stream = surface.GetReadStream();
                                if (stream.ReadByte() < 0)
                                {
                                    missing++;
                                    Mod.Log.Warn($"Prototype material audit: '{render.name}' slot {slot} has an empty surface '{surface.name}'.");
                                }
                            }
                            catch (Exception exception)
                            {
                                missing++;
                                Mod.Log.Warn(exception, $"Prototype material audit: '{render.name}' slot {slot}, '{surface.name}' is unreadable.");
                            }
                        }
                    }
                }
                Fields(prefab);
                foreach (var component in prefab.components) if (component != null) Fields(component);
                if (replacements.TryGetValue(prefab, out var children))
                    foreach (var child in children) pending.Enqueue(child);
            }
            catch (Exception exception)
            {
                missing++;
                Mod.Log.Warn(exception, $"Prototype material audit could not finish '{prefab.name}'.");
            }
        }
        Mod.Log.Info($"Prototype material audit: donors={roots.Length}; render prefabs including LODs={renders}; "
            + $"unique surfaces={surfaces.Count}; unresolved/unreadable={missing}. "
            + "Reference/file check only, not visual acceptance; shared assets were not modified.");
    }
}
