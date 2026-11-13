using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Runtime;

// Main-thread native registration. Entries intentionally outlive mod disposal:
// native meshes may still use them. Owner-file persistence is a separate stage.
internal static class BridgeCustomShaderRegistry
{
    private sealed class Program
    {
        internal readonly AssetBundle Bundle;
        internal readonly Shader Shader;
        internal Program(AssetBundle bundle, Shader shader) { Bundle = bundle; Shader = shader; }
    }

    private sealed class Entry
    {
        internal readonly MaterialLibrary Library;
        internal readonly string Identity;
        internal readonly MaterialLibrary.MaterialDescription Description;
        internal byte[]? Package;
        internal int SourceTemplate;
        internal int? RenderQueue;
        internal Entry(MaterialLibrary library, string identity, MaterialLibrary.MaterialDescription description)
        { Library = library; Identity = identity; Description = description; }
    }

    private static readonly Dictionary<string, Program> Programs = new(StringComparer.Ordinal);
    private static readonly List<Entry> Entries = new();
    private static readonly List<Entry> CompiledPackages = new();
    private static readonly Dictionary<string, byte[]> CompiledContainers = new(StringComparer.Ordinal);

    internal static bool TryGetProgram(MaterialLibrary library, int template,
        out byte[] package, out int sourceTemplate, out int? renderQueue)
    {
        foreach (var entry in CompiledPackages)
            if (ReferenceEquals(entry.Library, library) && entry.Description.m_Hash == template &&
                ReferenceEquals(library.GetMaterialDescription(template), entry.Description) && entry.Package != null)
            { package = entry.Package; sourceTemplate = entry.SourceTemplate; renderQueue = entry.RenderQueue; return true; }
        package = Array.Empty<byte>(); sourceTemplate = -1; renderQueue = null; return false;
    }

    internal static bool TryRegisterPackage(MaterialLibrary library, int sourceTemplate, byte[] package,
        out int template, out string? error)
        => TryRegisterConfiguredPackage(library, sourceTemplate, package, null, out template, out error);

    internal static bool TryRegisterConfiguredPackage(MaterialLibrary library, int sourceTemplate, byte[] package,
        int? renderQueue, out int template, out string? error)
    {
        template = -1; error = null;
        try
        {
            string identity;
            string packageIdentity;
            using (var hash = SHA256.Create()) packageIdentity = BitConverter.ToString(hash.ComputeHash(package));
            identity = packageIdentity + ":" + sourceTemplate + QueueIdentity(renderQueue);
            foreach (var entry in CompiledPackages)
                if (ReferenceEquals(entry.Library, library) && entry.Identity == identity)
                {
                    template = entry.Description.m_Hash; return true;
                }
            if (!CompiledContainers.TryGetValue(packageIdentity, out var container))
            {
                using var input = new MemoryStream(package, writable: false);
                if (!Bridges.MovableCustomShaderProgram.TryCompilePackage(input, out container, out error)) return false;
                CompiledContainers.Add(packageIdentity, container);
            }
            if (!TryRegisterCore(library, sourceTemplate, container, renderQueue, out template, out error, identity)) return false;
            CompiledPackages.Add(new Entry(library, identity, library.GetMaterialDescription(template))
                { Package = package, SourceTemplate = sourceTemplate, RenderQueue = renderQueue });
            return true;
        }
        catch (Exception exception)
        { template = -1; error = "Generated program preparation failed: " + exception.Message; return false; }
    }

    internal static bool TryRegister(MaterialLibrary library, int sourceTemplate, byte[] container,
        out int template, out string? error)
        => TryRegisterCore(library, sourceTemplate, container, null, out template, out error);

    private static string QueueIdentity(int? queue) => queue.HasValue ? ":queue=" + queue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";

    private static bool TryRegisterCore(MaterialLibrary library, int sourceTemplate, byte[] container,
        int? renderQueue, out int template, out string? error, string? persistedIdentity = null)
    {
        template = -1; error = null;
        AssetBundle? pendingBundle = null;
        Material? pendingMaterial = null;
        string? pendingProgram = null;
        try
        {
            var source = library.GetMaterialDescription(sourceTemplate);
            if (source == null || source.m_Material == null)
            { error = "Generated material source template is unavailable: " + sourceTemplate; return false; }
            string digest;
            using (var hash = SHA256.Create()) digest = BitConverter.ToString(hash.ComputeHash(container)).Replace("-", "");
            string identity = persistedIdentity ?? digest + ":" + sourceTemplate.ToString(System.Globalization.CultureInfo.InvariantCulture) + QueueIdentity(renderQueue);
            foreach (var entry in Entries)
                if (ReferenceEquals(entry.Library, library) && entry.Identity == identity)
                {
                    template = entry.Description.m_Hash; return true;
                }
            // Reserved positive content-derived identity; collisions are errors, never replacement.
            uint value = 2166136261;
            foreach (char c in identity) value = unchecked((value ^ c) * 16777619);
            int candidate = (int)(value & 0x3fffffff) | 0x40000000;
            if (library.GetMaterialDescription(candidate) != null)
            { error = "Generated material template identity collision: " + candidate; return false; }
            if (!Programs.TryGetValue(digest, out var program))
            {
                pendingBundle = AssetBundle.LoadFromMemory(container);
                if (pendingBundle == null)
                { error = "Generated material Unity container could not be loaded."; return false; }
                var shaders = pendingBundle.LoadAllAssets<Shader>();
                program = new Program(pendingBundle, shaders[0]);
                pendingProgram = digest;
            }
            pendingMaterial = new Material(program.Shader);
            pendingMaterial.CopyPropertiesFromMaterial(source.m_Material);
            // CopyPropertiesFromMaterial copies the source property sheet, which
            // does not contain newly declared object-protocol inputs. Establish
            // the generated bone property explicitly; native instance data replaces
            // this neutral range when the mechanism is batched.
            if (program.Shader.FindPropertyIndex("colossal_BoneParameters") >= 0 &&
                !source.m_Material.HasProperty("colossal_BoneParameters"))
                pendingMaterial.SetVector("colossal_BoneParameters", Vector4.zero);
            pendingMaterial.name = "BridgeBuilder Generated " + candidate;
            pendingMaterial.shaderKeywords = source.m_Material.shaderKeywords;
            pendingMaterial.renderQueue = renderQueue ?? source.m_Material.renderQueue;
            pendingMaterial.enableInstancing = source.m_Material.enableInstancing;
            pendingMaterial.doubleSidedGI = source.m_Material.doubleSidedGI;
            pendingMaterial.globalIlluminationFlags = source.m_Material.globalIlluminationFlags;
            for (int i = 0; i < source.m_Material.passCount; i++)
            {
                string pass = source.m_Material.GetPassName(i);
                if (pendingMaterial.FindPass(pass) >= 0)
                    pendingMaterial.SetShaderPassEnabled(pass, source.m_Material.GetShaderPassEnabled(pass));
            }
            var description = new MaterialLibrary.MaterialDescription
            {
                m_Hash = candidate, m_Material = pendingMaterial,
                m_SupportsVT = source.m_SupportsVT, m_MipBiasOverride = source.m_MipBiasOverride,
                m_Stacks = source.m_Stacks
            };
            // Publish only after all native construction succeeds.
            var registered = new Entry(library, identity, description);
            if (pendingProgram != null) Programs.Add(pendingProgram, program);
            try { library.m_Materials.Add(description); Entries.Add(registered); }
            catch
            {
                library.m_Materials.Remove(description); Entries.Remove(registered);
                if (pendingProgram != null) Programs.Remove(pendingProgram);
                throw;
            }
            pendingMaterial = null; pendingBundle = null;
            template = candidate; return true;
        }
        catch (Exception exception)
        { error = "Generated material registration failed: " + exception.GetType().Name + ": " + exception.Message; return false; }
        finally
        {
            try { if (pendingMaterial != null) UnityEngine.Object.Destroy(pendingMaterial); }
            catch (Exception exception) { error = (error ?? "Generated material cleanup failed.") + " Material release: " + exception.Message; }
            try { if (pendingBundle != null) pendingBundle.Unload(true); }
            catch (Exception exception) { error = (error ?? "Generated material cleanup failed.") + " Container release: " + exception.Message; }
        }
    }
}
