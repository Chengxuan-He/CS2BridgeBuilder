using System;
using System.Collections.Generic;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Runtime;

// Main-thread import of complete game-native material graphs. Published descriptions and their
// bundle stay alive while native render batches may reference them; no Material is reconstructed.
internal static class BridgeCustomShaderRegistry
{
    private sealed class Entry
    {
        internal readonly MaterialLibrary Library;
        internal readonly string Identity;
        internal readonly AssetBundle Bundle;
        internal readonly MaterialLibrary.MaterialDescription Description;
        internal Entry(MaterialLibrary library, string identity, AssetBundle bundle,
            MaterialLibrary.MaterialDescription description)
        { Library = library; Identity = identity; Bundle = bundle; Description = description; }
    }

    private static readonly List<Entry> Entries = new();

    internal static bool TryRegisterPackage(MaterialLibrary library, byte[] package, string identity,
        out int template, out string? error)
    {
        template = -1; error = null;
        AssetBundle? pending = null;
        MaterialLibrary.MaterialDescription? published = null;
        Entry? added = null;
        try
        {
            foreach (var entry in Entries)
                if (ReferenceEquals(entry.Library, library) && entry.Identity == identity)
                { template = entry.Description.m_Hash; return true; }

            // A native bundle can only be loaded once. A second library may publish the same
            // immutable description while retaining its already-loaded owning bundle.
            foreach (var entry in Entries)
                if (entry.Identity == identity)
                {
                    if (library.GetMaterialDescription(entry.Description.m_Hash) != null)
                    { error = "Generated material template identity collision: " + entry.Description.m_Hash; return false; }
                    added = new Entry(library, identity, entry.Bundle, entry.Description);
                    published = entry.Description;
                    library.m_Materials.Add(published);
                    Entries.Add(added);
                    template = published.m_Hash;
                    published = null; added = null;
                    return true;
                }

            pending = AssetBundle.LoadFromMemory(package);
            if (pending == null)
            { error = "Generated native material graph could not be loaded."; return false; }
            var graph = pending.LoadAllAssets<AssetDatabaseResources>()[0];
            var description = graph.materialLibrary.m_Materials[0];
            if (library.GetMaterialDescription(description.m_Hash) != null)
            { error = "Generated material template identity collision: " + description.m_Hash; return false; }

            // Preserve the serialized Material, property sheet, keywords/pass state, VT stacks and
            // mip metadata exactly. Only publication into the game's active library is necessary.
            added = new Entry(library, identity, pending, description);
            published = description;
            library.m_Materials.Add(description);
            Entries.Add(added);
            template = description.m_Hash;
            pending = null; published = null; added = null;
            return true;
        }
        catch (Exception exception)
        { error = "Generated native material registration failed: " + exception.GetType().Name + ": " + exception.Message; return false; }
        finally
        {
            if (published != null) library.m_Materials.Remove(published);
            if (added != null) Entries.Remove(added);
            try { if (pending != null) pending.Unload(true); }
            catch (Exception exception) { error = (error ?? "Generated material cleanup failed.") + " Container release: " + exception.Message; }
        }
    }
}
