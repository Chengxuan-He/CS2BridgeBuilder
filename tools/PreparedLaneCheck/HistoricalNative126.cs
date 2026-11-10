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
    internal static bool TryCreate(SurfaceAsset source, string owner, string styleId,
        PreviewGeometry? preview, List<AssetData> assets, out SurfaceAsset material, out string? error)
    {
        material = null!; error = null;
        string stage = "source-header", identity = "unresolved";
        try
        {
            identity = source.id.ToString();
            source.LoadHeader();
            if (MovableSurfaceAssets.IsObjectSurface(source))
            {
                stage = "object-vertex-protocol";
                if (!MovableSurfaceAssets.TryValidateObjectProtocol(source, out error)) return false;
                material = source; return true;
            }
            // Native cable descriptors can have different CIDs. Dispatch on their
            // shader contract, including in previews and all LODs, not one asset ID.
            if (source.materialTemplateHash == 30)
                return MovableCableSurface.TryCreate(source, owner, styleId, preview, assets, out material, out error);
            var target = source.materialTemplateHash switch
            {
                16 => 0, 31 => 1, 34 => 35,
                _ => -1
            };
            if (target < 0)
            {
                error = $"Material representation incomplete: stage={stage}; owner={owner}; style={styleId}; source={identity}; template={source.materialTemplateHash}; native object adapter unavailable. The source is not classified as corrupt.";
                return false;
            }
            stage = "template-capabilities";
            var library = AssetDatabase.global.resources.materialLibrary;
            var from = library.GetMaterialDescription(source.materialTemplateHash);
            var to = library.GetMaterialDescription(target);
            if (from.m_Stacks.Length != to.m_Stacks.Length ||
                from.m_Stacks.Where((stack, i) => !stack.m_PropertyNames.SequenceEqual(to.m_Stacks[i].m_PropertyNames)).Any())
            {
                error = $"Material representation incomplete: stage={stage}; owner={owner}; style={styleId}; source={identity}; native lane/object texture stacks differ.";
                return false;
            }
            stage = "source-descriptor-read";
            byte[] descriptor;
            using (var input = source.GetReadStream())
            using (var bytes = new MemoryStream()) { input.CopyTo(bytes); descriptor = bytes.ToArray(); }
            if (descriptor.Length < 6 || BitConverter.ToUInt16(descriptor, 0) != 1)
            {
                error = $"Material descriptor schema unavailable: stage={stage}; owner={owner}; style={styleId}; source={identity}; expected complete native version 1 header.";
                return false;
            }
            stage = "prepared-dependencies";
            var preparedResult = BridgePreparedMaterials.Lane(styleId, descriptor);
        if(!preparedResult.Success){error=preparedResult.Error;return false;}
        var prepared=preparedResult.Value!;
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
                    {
                        if (!MovableLaneMidMips.TryCompletePrepared(source, prepared, out error)) return false;
                        material = prepared; return true;
                    }
                }
                finally { source.UnloadProperties(); }
            }
            stage = "descriptor-registration";
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
                reader.ReadInt32();
                writer.Write(version);
                writer.Write(target);
                writer.Flush();
                input.CopyTo(output);
            }
            stage = "descriptor-properties";
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
                stage = "descriptor-save";
                result.SaveData(force: true);
                stage = "mip-dependencies";
                if (!MovableLaneMidMips.TryComplete(source, result, owner, preview, assets, out error)) return false;
                preview?.RetainSurfaceProperties(result);
            }
            finally { result.UnloadProperties(); }
            material = result; return true;
        }
        catch (Exception exception)
        {
            material = null!;
            error = $"Material operation failed: stage={stage}; owner={owner}; style={styleId}; source={identity}; " +
                exception.GetType().Name + ": " + exception.Message;
            return false;
        }
        finally
        {
            if (error != null) { material = null!; error = $"Material result: stage={stage}; owner={owner}; style={styleId}; source={identity}; {error}"; }
        }
    }

    private static readonly string[] AppearanceDefaults = { "_Snow", "_EmissiveIntensity", "_Metallic", "_Smoothness", "_NormalScale" };
    private static bool Same<T>(IReadOnlyDictionary<string, T> left, IReadOnlyDictionary<string, T> right)
        => left.Count == right.Count && left.All(p => right.TryGetValue(p.Key, out var value)
            && EqualityComparer<T>.Default.Equals(p.Value, value));
}
