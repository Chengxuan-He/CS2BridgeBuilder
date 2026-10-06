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
        internal bool CornerPatched, SchedulePatched, ScheduleObserved;
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
            Mod.Log.Info($"Rail seam repair installed for {system.FullName}: direct straight-track calculation, including partial geometry batches; no ownership or lane deletion.");
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
        if (!Backends.TryGetValue(typeof(T), out var backend)) return job.Schedule(list, batch, dependency);
        // Harmony's managed predicate is not executed by Burst. Complete the input producers,
        // partition the real job's write list, and run affected bridge edges directly through
        // that job's managed Execute. They never run through the unpatched Burst calculation.
        // Unrelated edges keep Burst; the downstream pipeline retains its ORIGINAL full list.
        dependency.Complete();
        if (!backend.ScheduleObserved)
        {
            backend.ScheduleObserved = true;
            Mod.Log.Info($"Rail seam schedule entered: backend={backend.SystemType.FullName}, batchEdges={list.Length}; installation alone is not a repair hit.");
        }
        BridgeRailSeamScope scope;
        try
        {
            scope = new BridgeRailSeamScope(system, job, list);
        }
        catch (Exception exception)
        {
            Mod.Log.Warn(exception, $"Rail seam topology inspection failed for {backend.SystemType.FullName}; native batch retained. The next batch will retry.");
            return job.Schedule(list, batch, dependency);
        }
        if (scope.Indices.Count == 0) return job.Schedule(list, batch, dependency);

        using var ordinary = new NativeList<Entity>(list.Length, Allocator.TempJob);
        using var bridges = new NativeList<Entity>(scope.Indices.Count, Allocator.TempJob);
        var selected = new HashSet<int>(scope.Indices);
        for (var i = 0; i < list.Length; i++)
            if (selected.Contains(i)) bridges.Add(list[i]); else ordinary.Add(list[i]);

        // Only job-local array bindings change. No entities, Owner components, lane buffers,
        // update flags, prefabs, or the caller's deferred list are modified by this adapter.
        if (ordinary.Length != 0) RunNative(job, backend, ordinary, batch, dependency);
        var snapshots = new List<BridgeRailSeamScope.GeometrySnapshot>();
        try
        {
            foreach (var entity in bridges)
                snapshots.Add(new BridgeRailSeamScope.GeometrySnapshot(system.EntityManager, entity));
            object boxed = job;
            backend.Entities.SetValue(boxed, bridges.AsArray());
            var calculation = (IJobParallelForDefer)boxed;
            _seams = scope.Seams;
            _visited = new HashSet<(Entity Edge, Entity Node)>();
            _hits = 0;
            for (var i = 0; i < bridges.Length; i++) calculation.Execute(i);
            // An unchanged neighbour is a read dependency, not an expected Execute call.
            // Do not reject partial updates merely because only one side was calculated.
            var audit = system.World.GetExistingSystemManaged<BridgeBuilder.Systems.BridgeRailSeamAuditSystem>();
            foreach (var seam in _visited) audit?.Observe(seam.Node);
            if (backend.Reports++ < 20)
                Mod.Log.Info($"Rail seam direct calculation: backend={backend.SystemType.FullName}, "
                    + $"batchEdges={list.Length}, straightEdges={bridges.Length}, nativeEdges={ordinary.Length}, "
                    + $"visitedEndpoints={_visited.Count}, predicateHits={_hits}; {scope.Evidence}");
        }
        catch (Exception exception)
        {
            // Preserve native failure behaviour for this batch, with explicit diagnostics.
            // A transient failure must not disable protection for all later recalculations.
            _seams = null;
            foreach (var snapshot in snapshots) snapshot.Restore(system.EntityManager);
            Mod.Log.Warn(exception, $"Rail seam direct calculation failed for {backend.SystemType.FullName}; native batch retained. The next batch will retry.");
            RunNative(job, backend, bridges, batch, dependency);
        }
        finally { _seams = null; _visited = null; }
        // Both partitions are complete before Flatten/Finish/NodeGeometry are scheduled.
        return dependency;
    }

    private static void RunNative<T>(T job, Backend backend, NativeList<Entity> entities, int batch,
        JobHandle dependency) where T : struct, IJobParallelForDefer
    {
        object boxed = job;
        backend.Entities.SetValue(boxed, entities.AsDeferredJobArray());
        ((T)boxed).Schedule(entities, batch, dependency).Complete();
    }
}
