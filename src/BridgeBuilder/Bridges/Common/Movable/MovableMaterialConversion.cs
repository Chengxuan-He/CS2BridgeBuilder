using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Preserve the selected lane's appearance, including its VT backing data. Only
// replace curve evaluation with native object skinning: positions are already baked.
internal static class MovableMaterialConversion
{
    internal static bool TryCreate(SurfaceAsset source, GeometryInput geometry, string owner, string styleId,
        PreviewGeometry? preview, List<AssetData> assets, out SurfaceAsset material, out string? error)
        => TryCreateMapped(source,geometry,owner,styleId,preview,assets,out material,out error,out _);

    internal static bool TryCreateMapped(SurfaceAsset source, GeometryInput geometry, string owner, string styleId,
        PreviewGeometry? preview, List<AssetData> assets, out SurfaceAsset material, out string? error,
        out MovableMaterialCompatibility.Mapping mapping)
    {
        material = null!; error = null; mapping=default;
        string stage = "source-header", identity = "unresolved";
        int assetStart = assets?.Count ?? 0;
        string ownership = owner;
        try
        {
        if (string.IsNullOrEmpty(owner)){error="Material output ownership is unavailable.";return false;}
        if (preview == null && BridgeAssetInfo.TryFileOwner(owner, out var permanent)) ownership = permanent;
        else if (preview != null && owner.StartsWith("tmp", StringComparison.Ordinal) && owner.Length >= 39) ownership = owner.Substring(0, 39);
            identity = source.id.ToString();
            source.LoadHeader();
            if (assets == null) { error = "Material output ownership is unavailable."; return false; }
            if (!MovableSurfaceAssets.TryReadSource(source, out _, out error)) return false;
            var library = AssetDatabase.global.resources.materialLibrary;
            int existingProgram = -1;
            if (BridgeCustomShaderRegistry.TryGetProgram(library, source.materialTemplateHash,
                out var program, out var originalTemplate, out var programQueue) &&
                !MovableCustomShaderAsset.TryPrepare(program, originalTemplate, owner, preview, assets,
                    out existingProgram, out error, programQueue)) return false;
            if (geometry.UnchangedNetworkInputs) { material = source; return true; }
            if (!geometry.EvaluatedPositions || !geometry.PackedNormals || geometry.BoneCount <= 0)
            { error = "Invalid receiving geometry: evaluated positions, packed normals and native procedural bones are required."; return false; }
            if (MovableSurfaceAssets.IsObjectSurface(source))
            {
                stage = "object-vertex-protocol";
                if (!MovableSurfaceAssets.TryValidateObjectProtocol(source, out var reason))
                    return MovableMaterialCompatibility.TryCreate(source,owner,styleId,preview,assets,reason??"Source protocol difference",out material,out mapping,out error,geometry.Projected);
                material = source; return true;
            }
            var sourceTemplate = library.GetMaterial(source.materialTemplateHash);
            // Compare the actual native protocol shader. A third-party template
            // identity is not an adapter selector. Alpha variants share this shader.
            int target = existingProgram;
            if (target >= 0) { stage = "registered-generated-program"; }
            else if (sourceTemplate.shader == library.GetMaterial(16).shader)
            {
                try
                {
                    source.LoadProperties(true);
                    var keywords = new HashSet<string>(sourceTemplate.shaderKeywords);
                    keywords.UnionWith(source.keywords.Where(keyword => sourceTemplate.shader.keywordSpace.FindKeyword(keyword).isValid));
                    if (keywords.Contains("_GPU_ANIMATION_NORMAL") || keywords.Contains("_GPU_ANIMATION_SHAPE"))
                    { var reason="Source animation buffer effects are frozen in evaluated object geometry."; return MovableMaterialCompatibility.TryCreate(source,owner,styleId,preview,assets,reason??"Source protocol difference",out material,out mapping,out error,geometry.Projected); }
                    target = keywords.Contains("_HASHED_ALPHATEST") ? (keywords.Contains("_DOUBLESIDED_ON") ? 36 : 35) : keywords.Contains("_ALPHATEST_ON") ? (keywords.Contains("_DOUBLESIDED_ON") ? 14 : 1) : (keywords.Contains("_DOUBLESIDED_ON") ? 13 : 0);
                }
                finally { source.UnloadProperties(); }
            }
            else if (sourceTemplate.shader == library.GetMaterial(17).shader)
            {
                // Preserve this shader's complete original layered pixel programs.
                // Only its evaluated geometry/current-skeleton vertex protocol changes.
                stage = "generated-world-layered-program";
                if (!MovableCustomShaderAsset.TryPrepareWorldLayered(owner, preview, assets, out target, out error)) return false;
            }

            if (target < 0)
            { var reason="Source shader has no exact object equivalent."; return MovableMaterialCompatibility.TryCreate(source,owner,styleId,preview,assets,reason??"Source protocol difference",out material,out mapping,out error,geometry.Projected); }
            stage = "template-capabilities";
            if (!TryValidatePair(sourceTemplate, library.GetMaterial(target), out var pairDifference))
            { var reason=pairDifference!; return MovableMaterialCompatibility.TryCreate(source,owner,styleId,preview,assets,reason??"Source protocol difference",out material,out mapping,out error,geometry.Projected); }
            var from = library.GetMaterialDescription(source.materialTemplateHash);
            var to = library.GetMaterialDescription(target);
            if (from.m_Stacks.Length != to.m_Stacks.Length ||
                from.m_Stacks.Where((stack, i) => !stack.m_PropertyNames.SequenceEqual(to.m_Stacks[i].m_PropertyNames)).Any())
            {
                var reason="Native lane/object texture stacks differ; selected source channels are materialized independently.";
                return MovableMaterialCompatibility.TryCreate(source,owner,styleId,preview,assets,reason??"Source protocol difference",out material,out mapping,out error,geometry.Projected);
            }
            stage = "source-descriptor-read";
            byte[] descriptor;
            using (var input = source.GetReadStream())
            using (var bytes = new MemoryStream()) { input.CopyTo(bytes); descriptor = bytes.ToArray(); }
            if (descriptor.Length < 6)
            {
                error = $"Material descriptor schema unavailable: stage={stage}; owner={owner}; style={styleId}; source={identity}; expected complete native version 1 header.";
                return false;
            }
            if(BitConverter.ToUInt16(descriptor,0)!=1)
            {var reason="Legacy source descriptor is realized through decoded semantic properties.";return MovableMaterialCompatibility.TryCreate(source,owner,styleId,preview,assets,reason??"Source protocol difference",out material,out mapping,out error,geometry.Projected);}
            // Existing prepared object descriptors enter through the same direct
            // object branch. Do not accept an older partial default-cache comparison
            // as equivalent to a newly inspected source template.
            stage = "descriptor-registration";
            var database = preview?.NativeAssets ?? AssetDatabase.user;
            var result = database.AddAsset<SurfaceAsset>(BridgePrefabPersistence.ResourcePath(
                owner + " Native Lane " + source.id, "Appearance"));
            assets.Add(result);
            // Copy the full native descriptor, not CopyFrom: CopyFrom omits the VT
            // header and VTSurface reference and would silently lose atlas bindings.
            if (!TryWriteDescriptor(descriptor, target, result, out error)) return false;
            bool propertiesHeld=false;
            stage = "descriptor-properties";
            try
            {
                propertiesHeld=true; result.LoadProperties(useVT: true);
                var template = library.GetMaterial(source.materialTemplateHash);
                // Copy declared shared appearance defaults, including custom template
                // defaults; serialized overrides already came from the full descriptor.
                if (!TryCopyDefaults(result, template, library.GetMaterial(target), out var reason))
                {
                    result.UnloadProperties(); propertiesHeld=false;
                    database.DeleteAsset(result); assets.Remove(result);
                    return MovableMaterialCompatibility.TryCreate(source,owner,styleId,preview,assets,reason??"Source protocol difference",out material,out mapping,out error,geometry.Projected);
                }
                result.UpdateKeywords(new HashSet<string>(result.keywords) { "_TANGENTSPACE_OCTO" });
                // SaveData preserves VT. Save(vt:false) rebuilds/clears the backing header.
                stage = "descriptor-save";
                result.SaveData(force: true);
                stage = "mip-dependencies";
                if (result.isVTMaterial && !MovableLaneMidMips.TryComplete(source, result, owner, preview, assets, out error)) return false;
                preview?.RetainSurfaceProperties(result);
            }
            finally { if(propertiesHeld)result.UnloadProperties(); }
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
            if (error != null && assets != null)
                for (int i = assets.Count - 1; i >= assetStart; i--)
                {
                    try
                    {
                        var asset = assets[i];
                        if (!MovableAssetOwnership.Matches(asset.path, ownership, preview) ||
                            (preview != null && !ReferenceEquals(asset.database, preview.NativeAssets)))
                        { error += " Conversion rollback refused an unowned resource: " + asset.path; continue; }
                        asset.database.DeleteAsset(asset); assets.RemoveAt(i);
                    }
                    catch (Exception cleanup) { error += " Conversion rollback failed: " + cleanup.Message; }
                }
            if (error != null) { material = null!; error = $"Material result: stage={stage}; owner={owner}; style={styleId}; source={identity}; {error}"; }
        }
    }

    internal static bool TryWriteDescriptor(byte[] descriptor, int target, SurfaceAsset result, out string? error)
    {
        error = null;
        try
        {
            if (descriptor == null || descriptor.Length < 6 || BitConverter.ToUInt16(descriptor, 0) != 1)
            { error = "Native Surface descriptor version is unavailable."; return false; }
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
            return true;
        }
        catch (Exception exception) { error = "Native Surface descriptor write failed: " + exception.Message; return false; }
    }

    internal readonly struct GeometryInput
    {
        internal readonly bool EvaluatedPositions, PackedNormals, UnchangedNetworkInputs, Projected;
        internal readonly int BoneCount;
        internal GeometryInput(bool evaluatedPositions, bool packedNormals, int boneCount)
            : this(evaluatedPositions,packedNormals,boneCount,false) { }
        internal GeometryInput(bool evaluatedPositions, bool packedNormals, int boneCount, bool projected)
        { EvaluatedPositions = evaluatedPositions; PackedNormals = packedNormals; BoneCount = boneCount; UnchangedNetworkInputs = false; Projected=projected; }
        private GeometryInput(bool nativeNetwork)
        { EvaluatedPositions = PackedNormals = false; BoneCount = 0; UnchangedNetworkInputs = nativeNetwork; Projected=false; }
        // Only an unchanged native NetObject graph may provide this context.
        internal static GeometryInput NativeNetwork => new GeometryInput(true);
    }

    private static bool TryValidatePair(UnityEngine.Material from, UnityEngine.Material to, out string? error)
    {
        error = null;
        if (from.renderQueue != to.renderQueue || from.doubleSidedGI != to.doubleSidedGI)
        { error = "Material representation incomplete: source render queue or sidedness cannot be persisted by the native object template."; return false; }
        for (int i = 0; i < from.passCount; i++)
        {
            var pass = from.GetPassName(i);
            if ((to.FindPass(pass) < 0 && from.GetShaderPassEnabled(pass)) ||
                (to.FindPass(pass) >= 0 && from.GetShaderPassEnabled(pass) != to.GetShaderPassEnabled(pass)))
            { error = "Material representation incomplete: native object pass state differs: " + pass; return false; }
        }
        foreach (var keyword in from.shaderKeywords)
        {
            if (keyword == "COLOSSAL_GEOMETRY_DEFAULT" || keyword == "COLOSSAL_GEOMETRY_TILING" || keyword == "COLOSSAL_GEOMETRY_DILATED") continue;
            if (!to.shader.keywordSpace.FindKeyword(keyword).isValid)
            { error = "Material representation incomplete: native object shader lacks source keyword: " + keyword; return false; }
        }
        return true;
    }

    private static Dictionary<string, T> SourceProperties<T>(IReadOnlyDictionary<string, T> values,
        UnityEngine.Material from, UnityEngine.Material to)
        // An override ignored by the source shader must not become a newly active
        // target-only property (for example an extra UV transform).
        => values.Where(pair => from.HasProperty(pair.Key) || !to.HasProperty(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

    internal static bool TryCopyDefaults(SurfaceAsset output,
        UnityEngine.Material from, UnityEngine.Material to, out string? error, bool evaluatedDecal = false)
    {
        error = null;
        var floats = SourceProperties(output.floats, from, to); var ints = SourceProperties(output.ints, from, to);
        var vectors = SourceProperties(output.vectors, from, to); var colors = SourceProperties(output.colors, from, to);
        var textures = SourceProperties(output.textures, from, to);
        for (int i = 0; i < from.shader.GetPropertyCount(); i++)
        {
            var name = from.shader.GetPropertyName(i);
            if (!to.HasProperty(name))
            {
                if (name == "colossal_CurveScale" || name == "colossal_CurveParams" || name == "colossal_HangingDistances" ||
                    name == "colossal_DilationParams" || name == "colossal_LodDistanceFactor") continue;
                // Net/object infoview values are supplied by their respective native
                // instance protocols, rather than an authored appearance layer.
                if (name == "colossal_NetInfoviewColor") continue;
                if (evaluatedDecal && name == "_SmoothingDistance") continue;
                // Editor RGB display field; the shader uses _EmissiveColor.
                if (name == "_EmissiveColorLDR") continue;
                if (name == "colossal_FlowOffset" && from.GetFloat(name) == 0 &&
                    (!output.floats.TryGetValue(name, out var flow) || flow == 0)) continue;
                error = "Material representation incomplete: source property has no object semantic: " + name;
                return false;
            }
            switch (from.shader.GetPropertyType(i))
            {
                case UnityEngine.Rendering.ShaderPropertyType.Float:
                case UnityEngine.Rendering.ShaderPropertyType.Range:
                    if (!output.floats.ContainsKey(name)) floats[name] = from.GetFloat(name); break;
                case UnityEngine.Rendering.ShaderPropertyType.Int:
                    if (!output.ints.ContainsKey(name)) ints[name] = from.GetInteger(name); break;
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    if (!output.colors.ContainsKey(name)) colors[name] = from.GetColor(name); break;
                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    if (!output.vectors.ContainsKey(name)) vectors[name] = from.GetVector(name); break;
                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    if (output.textures.ContainsKey(name) || from.GetTexture(name) == to.GetTexture(name)) break;
                    if (from.GetTexture(name) != null)
                    { error = "Material representation incomplete: a differing source-template texture default has no serialized native identity: " + name; return false; }
                    textures[name] = null!; break;
            }
        }
        output.UpdateFloats(floats); output.UpdateInts(ints); output.UpdateVectors(vectors);
        output.UpdateColors(colors); output.UpdateTextures(textures);
        var keywords = new HashSet<string>(from.shaderKeywords); keywords.UnionWith(output.keywords.Where(keyword => from.shader.keywordSpace.FindKeyword(keyword).isValid || !to.shader.keywordSpace.FindKeyword(keyword).isValid));
        keywords.Remove("COLOSSAL_GEOMETRY_DEFAULT"); keywords.Remove("COLOSSAL_GEOMETRY_TILING"); keywords.Remove("COLOSSAL_GEOMETRY_DILATED");
        foreach (var keyword in keywords)
            if (from.shader.keywordSpace.FindKeyword(keyword).isValid && !to.shader.keywordSpace.FindKeyword(keyword).isValid)
            { error = "Material representation incomplete: native object shader lacks source appearance keyword: " + keyword; return false; }
        keywords.Add("_TANGENTSPACE_OCTO"); output.UpdateKeywords(keywords);
        return true;
    }

}
