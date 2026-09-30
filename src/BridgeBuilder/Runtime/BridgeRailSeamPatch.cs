using Game;
using Game.Net;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Native GeometrySystem compares top owners when deciding whether to reserve rail crossover
/// space. Consecutive auxiliary tracks have different parent edges, unlike independent tracks
/// (both owners null). The 2026-09-28 rail-connections log records two-edge, four-track seams
/// with eight connections and 8m setbacks. Do not change actual Owner components to fix this.
/// </summary>
internal static class BridgeRailSeamPatch
{
    private const string HarmonyId = "BridgeBuilder.RailSeams";
    internal const string NodeControllerSystemName = "NodeController.Main.Systems.NcGeometrySystem";
    private static readonly Dictionary<Type, Backend> Backends = new();
    private static bool _active, _nodeControllerAttempted;
    private static int _nextDiscoveryFrame;

    private sealed class Backend
    {
        internal readonly Type SystemType, JobType;
        internal readonly FieldInfo Entities;
        internal readonly Harmony Harmony;
        internal bool CornerPatched, SchedulePatched, Disabled, ScheduleObserved;
        internal int Reports;
        internal Backend(Type system, Type job, FieldInfo entities)
        {
            SystemType = system;
            JobType = job;
            Entities = entities;
            Harmony = new Harmony(HarmonyId + "." + system.FullName);
        }
    }
    [ThreadStatic] private static HashSet<(Entity Edge, Entity Node)>? _seams;
    [ThreadStatic] private static HashSet<(Entity Edge, Entity Node)>? _visited;
    [ThreadStatic] private static int _hits;

    internal static void Start()
    {
        _active = true;
        _nodeControllerAttempted = false;
        _nextDiscoveryFrame = 0;
        BridgeRailSeamScope.ResetDiagnostics();
        Install(typeof(GeometrySystem));
        EnsureCompatiblePipelines();
    }

    internal static void EnsureCompatiblePipelines(bool forceDiscovery = false)
    {
        if (!_active || _nodeControllerAttempted || (!forceDiscovery && UnityEngine.Time.frameCount < _nextDiscoveryFrame)) return;
        _nextDiscoveryFrame = UnityEngine.Time.frameCount + 120;
        // Discovery is on the main thread, after optional mods can load. Do not load a DLL from
        // disk, require Node Controller, enable the disabled vanilla system, or replace NC jobs.
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "NodeController");
        if (assembly == null) return;
        _nodeControllerAttempted = true;
        var system = assembly.GetType(NodeControllerSystemName, false);
        if (system == null)
        {
            Mod.Log.Warn("Rail seam Node Controller adapter unavailable: geometry system not found.");
            return;
        }
        // Verified against installed NodeController.dll SHA256 AB3E4510CF70C9C131A42230AB961D668
        // 280B33392CAC085F0434D6C37E9DB85 (2026-09-30). Mod.OnLoad disables GeometrySystem and
        // schedules NcGeometrySystem instead. Its CalculateEdgeGeometryJob retains the same
        // GetTopOwner/CompareLanes predicate; its other jobs implement NC's node settings.
        Install(system);
    }

    private static void Install(Type system)
    {
        Backend? backend = null;
        try
        {
            var job = system.GetNestedType("CalculateEdgeGeometryJob", BindingFlags.NonPublic);
            var corner = job?.GetMethod("CalculateCornerOffset", BindingFlags.Instance | BindingFlags.NonPublic);
            var update = AccessTools.DeclaredMethod(system, "OnUpdate");
            var entities = job == null ? null : BridgeRailSeamScope.Field(job, "m_Entities");
            if (!typeof(GameSystemBase).IsAssignableFrom(system)
                || job == null || !typeof(IJobParallelForDefer).IsAssignableFrom(job)
                || corner == null || update == null || entities?.FieldType != typeof(NativeArray<Entity>)
                || !BridgeRailSeamScope.Supports(job))
            {
                Mod.Log.Warn($"Rail seam repair unavailable for {system.FullName}: job signature changed; pipeline retained.");
                return;
            }
            if (Backends.ContainsKey(job)) return;
            backend = new Backend(system, job, entities);
            Backends.Add(job, backend);
            backend.Harmony.Patch(corner, transpiler: new HarmonyMethod(typeof(BridgeRailSeamPatch), nameof(CornerTranspiler)));
            if (backend.CornerPatched)
                backend.Harmony.Patch(update, transpiler: new HarmonyMethod(typeof(BridgeRailSeamPatch), nameof(ScheduleTranspiler)));
            if (!backend.CornerPatched || !backend.SchedulePatched)
            {
                backend.Harmony.UnpatchAll(backend.Harmony.Id);
                Backends.Remove(job);
                Mod.Log.Warn($"Rail seam repair unavailable for {system.FullName}: IL pattern changed; pipeline retained.");
                return;
            }
            Mod.Log.Info($"Rail seam repair installed for {system.FullName}: scoped geometry replay before flattening/lane generation; no ownership or lane deletion.");
        }
        catch (Exception exception)
        {
            if (backend != null)
            {
                backend.Harmony.UnpatchAll(backend.Harmony.Id);
                Backends.Remove(backend.JobType);
            }
            Mod.Log.Warn(exception, $"Rail seam repair installation failed for {system.FullName}; pipeline retained.");
        }
    }

    internal static void Stop()
    {
        _active = false;
        foreach (var backend in Backends.Values) backend.Harmony.UnpatchAll(backend.Harmony.Id);
        Backends.Clear();
        _seams = null;
        _visited = null;
    }

    private static IEnumerable<CodeInstruction> CornerTranspiler(IEnumerable<CodeInstruction> instructions,
        MethodBase __originalMethod)
    {
        var code = instructions.ToList();
        if (__originalMethod.DeclaringType == null || !Backends.TryGetValue(__originalMethod.DeclaringType, out var backend)) return code;
        var calls = code.Where(i => i.operand is MethodInfo m && m.DeclaringType == backend.JobType
            && m.Name == "CompareLanes" && m.GetParameters().LastOrDefault()?.ParameterType == typeof(bool)).ToArray();
        var args = __originalMethod.GetParameters();
        if (calls.Length != 1 || args.Length < 2 || args[0].Name != "edge" || args[1].Name != "node"
            || args[0].ParameterType != typeof(Entity) || args[1].ParameterType != typeof(Entity)) return code;
        var index = code.IndexOf(calls[0]);
        // The existing bool is the last argument on the evaluation stack. Preserve every other
        // argument and every native slope/width/curve rule; only extend dontCrossTracks here.
        var loadEdge = new CodeInstruction(OpCodes.Ldarg_1);
        code[index].MoveLabelsTo(loadEdge);
        code.InsertRange(index, new[] { loadEdge, new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BridgeRailSeamPatch), nameof(KeepStraight))) });
        backend.CornerPatched = true;
        return code;
    }

    private static bool KeepStraight(bool native, Entity edge, Entity node)
    {
        if (_seams == null || !_seams.Contains((edge, node))) return native;
        _visited?.Add((edge, node));
        _hits++;
        return true;
    }

    private static IEnumerable<CodeInstruction> ScheduleTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var code = instructions.ToList();
        var backend = Backends.Values.FirstOrDefault(b => b.SystemType == __originalMethod.DeclaringType);
        if (backend == null) return code;
        var calls = code.Where(i => i.operand is MethodInfo m && m.IsGenericMethod
            && m.DeclaringType == typeof(IJobParallelForDeferExtensions) && m.Name == "Schedule"
            && m.GetGenericArguments().SequenceEqual(new[] { backend.JobType, typeof(Entity) })
            && m.GetParameters().Length == 4).ToArray();
        if (calls.Length != 1) return code;
        var index = code.IndexOf(calls[0]);
        var loadSystem = new CodeInstruction(OpCodes.Ldarg_0);
        code[index].MoveLabelsTo(loadSystem);
        code[index].operand = AccessTools.Method(typeof(BridgeRailSeamPatch), nameof(Schedule))!.MakeGenericMethod(backend.JobType);
        code.Insert(index, loadSystem);
        backend.SchedulePatched = true;
        return code;
    }

    private static JobHandle Schedule<T>(T job, NativeList<Entity> list, int batch, JobHandle dependency,
        GameSystemBase system) where T : struct, IJobParallelForDefer
    {
        // Schedule the REAL native struct, never an ABI/layout replica. Its Burst pass remains
        // intact. Complete it before reading lookups or replaying a write, and return the same
        // dependency to native Flatten/Finish/NodeGeometry jobs. No ECS structural changes here.
        var handle = job.Schedule(list, batch, dependency);
        if (!Backends.TryGetValue(typeof(T), out var backend) || backend.Disabled) return handle;
        handle.Complete();
        if (!backend.ScheduleObserved)
        {
            backend.ScheduleObserved = true;
            Mod.Log.Info($"Rail seam schedule entered: backend={backend.SystemType.FullName}, batchEdges={list.Length}; installation alone is not a repair hit.");
        }
        var snapshots = new List<BridgeRailSeamScope.GeometrySnapshot>();
        try
        {
            object boxed = job;
            var scope = new BridgeRailSeamScope(system, boxed, list);
            if (scope.Seams.Count == 0) return handle;
            // AsDeferredJobArray is patched only inside the scheduler's copy. A managed replay
            // MUST use a resolved array, not that deferred sentinel or a hand-built native pointer.
            backend.Entities.SetValue(boxed, list.AsArray());
            var replay = (IJobParallelForDefer)boxed;
            foreach (var index in scope.Indices)
                snapshots.Add(new BridgeRailSeamScope.GeometrySnapshot(system.EntityManager, list[index]));
            _seams = scope.Seams;
            _visited = new HashSet<(Entity Edge, Entity Node)>();
            _hits = 0;
            foreach (var index in scope.Indices) replay.Execute(index);
            if (!scope.Seams.IsSubsetOf(_visited))
            {
                foreach (var snapshot in snapshots) snapshot.Restore(system.EntityManager);
                backend.Disabled = true;
                Mod.Log.Warn("Rail seam replay did not reach both sides of every seam; native geometry restored, repair disabled.");
            }
            else
            {
                var audit = system.World.GetExistingSystemManaged<BridgeBuilder.Systems.BridgeRailSeamAuditSystem>();
                foreach (var seam in scope.Seams) audit?.Observe(seam.Node);
                if (backend.Reports++ < 20)
                    Mod.Log.Info($"Rail seam repair: backend={backend.SystemType.FullName}, {scope.Seams.Count / 2} internal seam(s), "
                        + $"{scope.Indices.Count} native edge replay(s), {_hits} predicate hit(s); {scope.Evidence}");
            }
        }
        catch (Exception exception)
        {
            // A replay is a geometry-only transaction. Restore the native results for the whole
            // batch if anything fails; never leave only one deck corrected, retry every frame,
            // swallow a native producer failure, or destroy a network/lane to hide the error.
            backend.Disabled = true;
            foreach (var snapshot in snapshots) snapshot.Restore(system.EntityManager);
            Mod.Log.Warn(exception, "Rail seam replay failed; native geometry restored, repair disabled for this session.");
        }
        finally { _seams = null; _visited = null; }
        return handle;
    }
}
