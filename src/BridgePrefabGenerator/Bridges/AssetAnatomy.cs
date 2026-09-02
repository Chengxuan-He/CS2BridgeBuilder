using CS2Mods.Shared;
using CS2Mods.Shared.Export;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// Writes out everything a prefab is made of, by reflection, all the way down.
///
/// Nothing is chosen. Anything that is not a primitive is opened and its own contents printed, so what
/// appears is what the types actually have. Every version of this that decided what was worth printing
/// hid the difference that mattered: a sub object entry printed as eight of its twelve fields, and the
/// three that went unprinted were three that went unwritten; a mesh's bounds printed as the letters
/// "Colossal.Mathematics.Bounds3", and the bounds are what the game reads to place a pillar.
///
/// It runs on every catalogue rebuild, so what is bounding it matters. Three things do, and they are
/// not the same thing.
///
/// A depth of sixteen bounds how far it descends, counting every step rather than prefab hops alone.
/// Counting only the hops let structs and info objects nest without limit on the reasoning that they
/// are small - they are, and there are a great many of them.
///
/// The chain of objects currently being expanded stops cycles: an object that is one of its own
/// ancestors would not terminate, so it is named and left.
///
/// A set of what this bridge has already shown stops repeats. Without it the walk is finite and still
/// unusable - a bridge names prefabs that name prefabs, and printing each occurrence in full multiplies
/// until one bridge fills the file and the second is never reached. The set is per bridge, not global:
/// each bridge is expanded completely on its own terms, so the two can still be read against each
/// other, which is the whole point of the file. A global set would make the second bridge a page of
/// "listed above" and two bridges look alike whether or not they were.
/// </summary>
internal static class AssetAnatomy
{
    /// <summary>
    /// The two bridges the archetype was measured from. Everything else in the file is a bridge this
    /// mod generated, read out of the export state.
    ///
    /// Only these two, because the file exists to be diffed. A dump covering one bridge of every type
    /// is a survey - useful once, for filling in measurements - and a survey is the wrong shape for
    /// the question being asked now, which is where a generated bridge differs from the one it copies.
    /// The towers are not listed: they are reached through the bridges that carry them, and listing
    /// them again would print each twice.
    /// </summary>
    /// <summary>
    /// Which bridges are dumped: every archetype the generator can build from, and everything it has
    /// built.
    ///
    /// It used to name two prefabs. Two was right while the suspension family was the only one being
    /// worked on and wrong the moment anything else was, because a fault in a family the file does not
    /// cover is a fault nothing can be diffed against - and rule 7 says the first move is to dump both
    /// and diff, not to reason about it. The archetypes are found rather than listed, so a family that
    /// is installed is a family that is covered.
    /// </summary>
    private static IEnumerable<string> RootsOf(IEnumerable<PrefabBase> catalogue)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Every donor of every style, which is what the generator would derive from.
        foreach (var style in BridgeStyleCatalog.Styles)
        {
            foreach (var variant in style.Variants)
            {
                if (variant.Donor != null && seen.Add(variant.Donor.name)) names.Add(variant.Donor.name);
            }
        }

        // And everything generated so far, so the two can be read side by side.
        try
        {
            foreach (var name in ExportStateStore.Load().ExportNames())
            {
                if (seen.Add(name)) names.Add(name);
            }
        }
        catch (Exception)
        {
            // Reported by the caller; a dump of the archetypes alone is still worth writing.
        }

        return names;
    }
    /// <summary>
    /// How many levels of anything the walk may descend.
    ///
    /// One limit, counting every step: into a field, into an array element, into a component, into a
    /// prefab. It replaces a budget that counted prefab hops alone and let everything else nest freely,
    /// on the reasoning that structs and info objects are small. They are; there are just a great many
    /// of them, and unbounded nesting through them reached far enough to exhaust the process twice.
    ///
    /// Twelve is enough for what this file is for. The longest path that matters runs bridge, sub
    /// objects, the entry, the tower, its meshes, the mesh entry, the render prefab, its bounds - eight
    /// levels, with room to spare.
    /// </summary>
    private const int MaxDepth = 12;


    /// <summary>
    /// How many render prefabs will have their meshes loaded, per bridge.
    ///
    /// Obtaining meshes is not reading a field: it asks the asset database to load geometry, and a
    /// bridge can reach hundreds of render prefabs. Loading all of them to print a line about each is
    /// what a diagnostic has no business doing to a running game.
    ///
    /// Per bridge, not per dump. One budget for the whole file meant the bridges listed first spent
    /// it and the generated one reached its own geometry with none left, so the only meshes the
    /// metadata ever described were the ones already known to be right.
    /// </summary>
    private const int MaxMeshLoads = 96;

    /// <summary>
    /// The only properties read off a Unity object.
    ///
    /// Reflection over every property of a live engine object calls into native code with no idea what
    /// it is asking for, and some of those calls do not come back. These are the ones that hold what
    /// this file is for - a render prefab keeps its geometry in properties, and its bounds are the
    /// number the game places a pillar by.
    /// </summary>
    private static int _meshLoads;

    private static readonly HashSet<string> SafeProperties = new(StringComparer.Ordinal)
    {
        "bounds", "vertexCount", "indexCount", "meshCount", "surfaceArea", "geometryAsset",
        "surfaceAssets", "isImpostor", "manualVTRequired", "name",
    };

    internal static void Run(PrefabSystem prefabSystem)
    {
        _meshLoads = 0;

        string path;
        try
        {
            ExportPaths.EnsureDataDirectory();
            path = Path.Combine(ExportPaths.DataDirectory, "asset-anatomy.txt");
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn($"Could not prepare the asset anatomy: {exception.Message}");
            return;
        }

        try
        {
            // Opened first and written through. Collecting the lines and saving at the end held every
            // one of them live until the last was produced, which is what ran the game out of memory.
            using var writer = new StreamWriter(path, false);
            var lines = new Sink(writer);

            lines.Add("# What these prefabs are made of, read by reflection out of the loaded game.");
            lines.Add("#");
            lines.Add("# Anything that is not a primitive is opened: prefabs, components, the info objects");
            lines.Add("# they hold, the structs inside those, and for anything carrying geometry the");
            lines.Add("# properties that hold it and the vertex layout of its meshes. Nothing is selected.");
            lines.Add("#");
            lines.Add("# A line reading (already expanding) is a cycle - the object is one of its own");
            lines.Add("# ancestors here. A line reading (shown above) is a repeat within the same bridge.");
            lines.Add("#");
            lines.Add("# Two bridges the game ships, and every bridge this mod generated. A generated");
            lines.Add("# bridge should differ from the archetype it copies in its road sections and in mesh");
            lines.Add("# contents, and in nothing else.");
            lines.Add("#");
            lines.Add("# Generated: " + DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture));
            lines.Add(string.Empty);

            var catalogue = PrefabCatalog.GetAll(prefabSystem).ToArray();
            var all = new Replacements(catalogue);

            var wanted = new List<string>(RootsOf(catalogue));
            if (wanted.Count == 0)
            {
                lines.Add("# No bridge styles have been scanned, so there is nothing to dump.");
            }

            foreach (var name in wanted)
            {
                var prefab = catalogue.FirstOrDefault(candidate =>
                    candidate != null && string.Equals(candidate.name, name, StringComparison.Ordinal));
                if (prefab == null)
                {
                    lines.Add($"### {name} - not installed");
                    lines.Add(string.Empty);
                    continue;
                }

                // A fresh set per bridge: what one bridge showed does not stop the next showing it.
                // The mesh budget resets with it, for the same reason and a sharper one: it used to
                // be one budget for the whole dump, so the bridges read first spent it and the
                // generated bridge - the only one being diagnosed - reached its own geometry with
                // nothing left. Every layout line in the file described a mesh that was already
                // known to be right.
                var shown = new HashSet<object>(ReferenceComparer.Instance) { prefab };
                _meshLoads = 0;

                var before = lines.Count;
                Expand(prefab, lines, string.Empty, 0, new List<object>(), shown, all);
                ModHost.Log.Info(
                    $"Asset anatomy: {prefab.name} took {lines.Count - before} line(s), "
                    + $"{shown.Count} prefab(s).");
                lines.Add(string.Empty);
            }

            ModHost.Log.Info($"Asset anatomy written to {path} ({lines.Count} lines)");
        }
        catch (Exception exception)
        {
            // Whatever was reached is on disk already, which is the point of writing through.
            ModHost.Log.Warn($"Asset anatomy stopped early: {exception.GetType().Name}: {exception.Message}");
        }
    }

    /// <summary>
    /// Prints one value, opening it if it is not a primitive.
    ///
    /// <paramref name="chain"/> is the objects currently being expanded, outermost first. An object
    /// already on it would be expanding into itself, so it is named and left; anything else is opened,
    /// however many times it has appeared elsewhere.
    /// </summary>
    private static void Value(
        string label,
        object? value,
        Sink lines,
        string pad,
        int depth,
        List<object> chain,
        HashSet<object> shown,
        Replacements all)
    {
        if (value == null)
        {
            lines.Add($"{pad}{label} = null");
            return;
        }

        if (depth >= MaxDepth)
        {
            lines.Add($"{pad}{label} = <depth {MaxDepth} reached>");
            return;
        }

        var type = value.GetType();
        if (IsLeaf(type))
        {
            lines.Add($"{pad}{label} = {Text(value)}");
            return;
        }

        // Unity treats a destroyed object as equal to null while it is still a live reference, so the
        // null test above can pass something that cannot be read. Name it and stop.
        if (value is UnityEngine.Object unityObject && unityObject == null)
        {
            lines.Add($"{pad}{label} = <destroyed {type.Name}>");
            return;
        }

        // Reference identity, not equality: two prefabs can compare equal to Unity while being
        // different objects, and a chain built on equality would refuse to open the second.
        if (chain.Any(entry => ReferenceEquals(entry, value)))
        {
            lines.Add($"{pad}{label} -> {Name(value)} ({type.Name}) (already expanding)");
            return;
        }

        // Already shown for this bridge: name it and move on. Opening it again would say nothing new
        // and, because prefabs name prefabs, would multiply until one bridge filled the file.
        if (value is PrefabBase && !shown.Add(value))
        {
            lines.Add($"{pad}{label} -> {Name(value)} ({type.Name}) (shown above)");
            return;
        }

        if (value is IEnumerable list && value is not string)
        {
            var items = list.Cast<object?>().ToArray();
            lines.Add($"{pad}{label} = [{items.Length}]");

            chain.Add(value);
            for (var index = 0; index < items.Length; index++)
            {
                Value($"[{index}]", items[index], lines, pad + "  ", depth + 1, chain, shown, all);
            }

            chain.RemoveAt(chain.Count - 1);
            return;
        }

        lines.Add($"{pad}{label}: {Name(value)} ({type.Name})");
        chain.Add(value);
        Members(value, lines, pad + "  ", depth + 1, chain, shown, all);
        chain.RemoveAt(chain.Count - 1);
    }

    /// <summary>
    /// Everything one object holds: its serialized fields, its readable properties, and - where it is a
    /// prefab - its components and the objects a placeholder turns into.
    ///
    /// Properties are read as well as fields because a render prefab keeps its geometry in them. The
    /// bounds, the surfaces, the vertex and index counts are all properties, and a dump of fields alone
    /// showed none of them while looking complete.
    /// </summary>
    private static void Members(
        object owner,
        Sink lines,
        string pad,
        int depth,
        List<object> chain,
        HashSet<object> shown,
        Replacements all)
    {
        foreach (var field in SerializedFields.Of(owner.GetType()))
        {
            if (field.Name == nameof(PrefabBase.components)) continue;

            try
            {
                Value(field.Name, field.GetValue(owner), lines, pad, depth, chain, shown, all);
            }
            catch (Exception exception)
            {
                lines.Add($"{pad}{field.Name} = <unreadable: {exception.GetType().Name}>");
            }
        }

        // Properties are read only where the data lives in them, and only the recorded ones.
        //
        // A field is state; a property is a computation. Reading every property of every object was
        // both dangerous and non-terminating: a live engine object answers some of them by calling
        // into native code, and an ordinary object can answer with a freshly allocated result every
        // time - which is a new reference, so neither the chain nor the shown set recognises it, and
        // the walk expands it again on every encounter and never converges. That is what hung the game.
        //
        // A render prefab keeps its geometry in properties, and its bounds are the number the game
        // places a pillar by, so those are asked for by name. Everything else is read as fields.
        if (owner is RenderPrefab)
        {
            foreach (var name in SafeProperties)
            {
                var property = owner.GetType().GetProperty(
                    name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null || !property.CanRead) continue;

                try
                {
                    Value("(" + name + ")", property.GetValue(owner), lines, pad, depth, chain, shown, all);
                }
                catch (Exception exception)
                {
                    lines.Add($"{pad}({name}) = <unreadable: {exception.GetType().Name}>");
                }
            }
        }

        if (owner is RenderPrefab render) Meshes(render, lines, pad);
        if (owner is not PrefabBase prefab) return;

        foreach (var component in prefab.components)
        {
            if (component == null) continue;
            Value("<" + component.GetType().Name + ">", component, lines, pad, depth, chain, shown, all);
        }

        // What a placeholder turns into. Followed because the tower a bridge names is only half of one:
        // the placeholder carries the shaft, and the parts reaching the ground are on the replacement.
        if (!prefab.Has<PlaceholderObject>()) return;

        // Looked up rather than searched. This used to walk every prefab in the game asking each
        // whether it stood in for this placeholder - tens of thousands of them, once per placeholder
        // reached - which is slow enough to look like a hang on its own.
        if (!all.TryGetValue(prefab, out var replacements)) return;

        lines.Add($"{pad}[replacements] {replacements.Count}");
        for (var index = 0; index < replacements.Count; index++)
        {
            Value($"[replacement {index}]", replacements[index], lines, pad + "  ", depth, chain, shown, all);
        }
    }

    /// <summary>
    /// Prints one prefab as a heading rather than as a field, for the ones asked for by name.
    /// </summary>
    private static void Expand(
        PrefabBase prefab,
        Sink lines,
        string pad,
        int depth,
        List<object> chain,
        HashSet<object> shown,
        Replacements all)
    {
        lines.Add($"{pad}### {prefab.name} ({prefab.GetType().Name})");
        chain.Add(prefab);
        Members(prefab, lines, pad + "  ", depth + 1, chain, shown, all);
        chain.RemoveAt(chain.Count - 1);
    }

    /// <summary>
    /// The vertex layout of a render prefab's meshes.
    ///
    /// Not reachable by reflection - the meshes are behind a call, not a property - and the one thing
    /// the cables needed. A net piece declares its normals as two components of signed normalised
    /// sixteen-bit and its tangents as a single float; a mesh rebuilt through Unity's convenience
    /// accessors declares three floats and four. The renderer reads each vertex at a stride it computes
    /// from the declaration, so the difference is every channel after the first being read from the
    /// wrong place.
    /// </summary>

    /// <summary>Unity's vertex array as plain points, so the span rule needs nothing from the engine.</summary>
    private static float3[] ToPoints(Vector3[] vertices)
    {
        var points = new float3[vertices.Length];
        for (var index = 0; index < vertices.Length; index++)
        {
            points[index] = new float3(vertices[index].x, vertices[index].y, vertices[index].z);
        }

        return points;
    }

    private static void Meshes(RenderPrefab render, Sink lines, string pad)
    {
        if (_meshLoads >= MaxMeshLoads)
        {
            lines.Add($"{pad}[mesh] not loaded: {MaxMeshLoads} render prefabs already read");
            return;
        }

        _meshLoads++;

        Mesh[]? loaded = null;
        try
        {
            loaded = render.ObtainMeshes();
            for (var index = 0; index < (loaded?.Length ?? 0); index++)
            {
                var mesh = loaded![index];
                if (mesh == null) continue;

                var layout = mesh.GetVertexAttributes().Select(attribute => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}x{2}@{3}",
                    attribute.attribute, attribute.format, attribute.dimension, attribute.stream));

                // The gap between the legs, which bounds cannot report: a box has an outer face and no
                // inner one. Found by slicing the mesh across its height, because the crossbeams run
                // through the middle and would otherwise answer for the legs.
                var span = TowerWidening.ClearSpanOf(
                    ToPoints(mesh.vertices), TowerWidening.SpanBands);

                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}[mesh {1}] vertices={2} submeshes={3} bounds={4} clearSpan={5:0.#####} layout=[{6}]",
                    pad, index, mesh.vertexCount, mesh.subMeshCount, mesh.bounds, span,
                    string.Join(", ", layout)));
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var descriptor = mesh.GetSubMesh(sub);
                    lines.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}[mesh {1}] submesh {2} topology={3} indexStart={4} indexCount={5} baseVertex={6}",
                        pad, index, sub, descriptor.topology, descriptor.indexStart,
                        descriptor.indexCount, descriptor.baseVertex));
                }
            }
        }
        catch (Exception exception)
        {
            lines.Add($"{pad}[mesh] could not be read: {exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            if (loaded != null)
            {
                try
                {
                    render.ReleaseMeshes();
                }
                catch (Exception)
                {
                    // Releasing is a courtesy to the asset cache, not a correctness requirement.
                }
            }
        }
    }

    /// <summary>
    /// Whether a value is printed as itself rather than opened.
    ///
    /// Primitives, enums, strings and the handful of framework types that describe themselves properly.
    /// Everything else is opened - including structs, which is how the bounds came to be printed as the
    /// name of their type for as long as they were.
    /// </summary>
    private static bool IsLeaf(Type type)
    {
        if (type.IsPrimitive || type.IsEnum) return true;
        if (type == typeof(string) || type == typeof(decimal)) return true;
        if (type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid)) return true;

        // A collection is never a leaf, whatever it can say about itself.
        //
        // An array has no fields of its own, so the test below called every one of them a leaf and
        // printed it as "Game.Prefabs.NetSectionInfo[]". Nothing recursed after that - a bridge's
        // sections, its sub objects, its overhead sections each collapsed to a single line - and the
        // dump came out a twentieth of its size while still reading like a dump.
        if (typeof(IEnumerable).IsAssignableFrom(type)) return false;

        // Nothing to open: a type with no fields prints as whatever its ToString says.
        return !SerializedFields.Of(type).Any();
    }

    private static string Name(object value)
    {
        return value switch
        {
            PrefabBase prefab => prefab.name,
            UnityEngine.Object unityObject => unityObject.name,
            _ => value.GetType().Name,
        };
    }

    /// <summary>
    /// Identity, not equality.
    ///
    /// Unity compares a destroyed object equal to null and can compare two live prefabs equal to each
    /// other, so a set keyed on Equals would refuse to show the second of a pair that only looks like
    /// the first - which is exactly the pair worth looking at.
    /// </summary>
    /// <summary>
    /// Which objects stand in for which placeholder, worked out once.
    ///
    /// Built by walking every prefab a single time and filing each spawnable under the placeholders it
    /// names. The walk used to answer the same question by searching the whole catalogue afresh for
    /// every placeholder it met, which is a scan of tens of thousands of prefabs repeated as many times
    /// as there are towers - slow enough on its own to look like the game had stopped.
    /// </summary>
    private sealed class Replacements
    {
        private readonly Dictionary<object, List<ObjectGeometryPrefab>> _byPlaceholder =
            new(ReferenceComparer.Instance);

        internal Replacements(PrefabBase[] all)
        {
            foreach (var candidate in all)
            {
                if (candidate is not ObjectGeometryPrefab geometry) continue;
                if (!geometry.TryGet<SpawnableObject>(out var spawnable)) continue;
                if (spawnable?.m_Placeholders == null) continue;

                foreach (var placeholder in spawnable.m_Placeholders)
                {
                    if (placeholder == null) continue;

                    if (!_byPlaceholder.TryGetValue(placeholder, out var list))
                    {
                        list = new List<ObjectGeometryPrefab>();
                        _byPlaceholder[placeholder] = list;
                    }

                    list.Add(geometry);
                }
            }
        }

        internal bool TryGetValue(PrefabBase placeholder, out List<ObjectGeometryPrefab> replacements)
        {
            return _byPlaceholder.TryGetValue(placeholder, out replacements!);
        }
    }

    /// <summary>
    /// Lines on their way to the file, written as they are produced.
    ///
    /// They used to be collected in a list and written at the end, which is what ran the game out of
    /// memory: a walk that reaches a few million lines holds a few million strings, all of them live
    /// until the last one is produced. Writing through keeps the cost of the dump constant however long
    /// it turns out to be, and means a walk that dies partway still leaves what it had reached.
    /// </summary>
    private sealed class Sink
    {
        private readonly StreamWriter _writer;

        internal Sink(StreamWriter writer) => _writer = writer;

        internal int Count { get; private set; }

        internal void Add(string line)
        {
            _writer.WriteLine(line);
            Count++;
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new();

        public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);

        public int GetHashCode(object value) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }

    private static string Text(object value)
    {
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "?";
    }
}
