using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Preserve the selected lane's appearance, including its VT backing data. Only
// replace curve evaluation with native object skinning: positions are already baked.
internal static class MovableLaneSurface
{
    internal static SurfaceAsset Create(SurfaceAsset source, string owner, string styleId,
        PreviewGeometry? preview, List<AssetData> assets)
    {
        source.LoadHeader();
        if (MovableSurfaceAssets.IsObjectSurface(source)) return source;
        // Native cable descriptors can have different CIDs. Dispatch on their
        // shader contract, including in previews and all LODs, not one asset ID.
        if (source.materialTemplateHash == 30)
            return MovableCableSurface.Create(source, owner, styleId, preview, assets);
        var target = source.materialTemplateHash switch
        {
            16 => 0, 31 => 1, 34 => 35,
            _ => throw new NotSupportedException("Unsupported native lane material: " + source.name)
        };
        var library = AssetDatabase.global.resources.materialLibrary;
        var from = library.GetMaterialDescription(source.materialTemplateHash);
        var to = library.GetMaterialDescription(target);
        if (from.m_Stacks.Length != to.m_Stacks.Length ||
            from.m_Stacks.Where((stack, i) => !stack.m_PropertyNames.SequenceEqual(to.m_Stacks[i].m_PropertyNames)).Any())
            throw new InvalidOperationException("Native lane/object texture stacks are incompatible.");
        byte[] descriptor;
        using (var input = source.GetReadStream())
        using (var bytes = new MemoryStream()) { input.CopyTo(bytes); descriptor = bytes.ToArray(); }
        var prepared = BridgePreparedMaterials.Lane(styleId, descriptor);
        if (prepared != null)
        {
            source.LoadProperties(useVT: true);
            try
            {
                var expected = new Dictionary<string, float>(source.floats);
                var template = library.GetMaterial(source.materialTemplateHash);
                foreach (var property in AppearanceDefaults)
                    if (!expected.ContainsKey(property) && template.HasProperty(property)) expected.Add(property, template.GetFloat(property));
                if (prepared.materialTemplateHash == target && Same(expected, prepared.floats)
                    && Same(source.ints, prepared.ints) && Same(source.vectors, prepared.vectors)
                    && Same(source.colors, prepared.colors)
                    && new HashSet<string>(source.keywords) { "_TANGENTSPACE_OCTO" }.SetEquals(prepared.keywords)
                    && source.textures.Count == prepared.textures.Count
                    && source.textures.All(p => prepared.textures.TryGetValue(p.Key, out var value) && Equals(p.Value?.id, value?.id)))
                    return prepared;
            }
            finally { source.UnloadProperties(); }
        }
        var database = preview?.NativeAssets ?? AssetDatabase.user;
        var result = database.AddAsset<SurfaceAsset>(BridgePrefabPersistence.ResourcePath(
            owner + " Native Lane " + source.id, "Appearance"));
        assets.Add(result);
        // Copy the full native descriptor, not CopyFrom: CopyFrom omits the VT
        // header and VTSurface reference and would silently lose atlas bindings.
        using (var input = new MemoryStream(descriptor, writable: false))
        using (var reader = new BinaryReader(input))
        using (var output = result.GetWriteStream())
        using (var writer = new BinaryWriter(output))
        {
            var version = reader.ReadUInt16();
            if (version != 1) throw new NotSupportedException("Unsupported native lane surface version: " + version);
            reader.ReadInt32();
            writer.Write(version);
            writer.Write(target);
            writer.Flush();
            input.CopyTo(output);
        }
        result.LoadProperties(useVT: true);
        try
        {
            var template = library.GetMaterial(source.materialTemplateHash);
            var defaults = new Dictionary<string, float>();
            foreach (var property in AppearanceDefaults)
                if (!result.floats.ContainsKey(property) && template.HasProperty(property))
                    defaults.Add(property, template.GetFloat(property));
            result.UpdateFloats(defaults);
            result.UpdateKeywords(new HashSet<string>(result.keywords) { "_TANGENTSPACE_OCTO" });
            // SaveData preserves VT. Save(vt:false) rebuilds/clears the backing header.
            result.SaveData(force: true);
            preview?.RetainSurfaceProperties(result);
        }
        finally { result.UnloadProperties(); }
        return result;
    }

    private static readonly string[] AppearanceDefaults = { "_Snow", "_EmissiveIntensity", "_Metallic", "_Smoothness", "_NormalScale" };
    private static bool Same<T>(IReadOnlyDictionary<string, T> left, IReadOnlyDictionary<string, T> right)
        => left.Count == right.Count && left.All(p => right.TryGetValue(p.Key, out var value)
            && EqualityComparer<T>.Default.Equals(p.Value, value));
}
