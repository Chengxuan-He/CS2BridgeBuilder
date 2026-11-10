using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Colossal.IO.AssetDatabase;
using Game.SceneFlow;

namespace BridgeBuilder.Runtime;

// Unity's final quit event survives other mods' ownerless Harmony.UnpatchAll calls.
internal static class BridgeShutdownCleanup
{
    private static bool _normalShutdown;
    private static string _root = "", _diagnosticPath = "";
    private static GameManager? _manager;
    private static AssetDatabase? _database;
    private static Colossal.TaskManager? _tasks;
    private static Task[] _shutdownTasks = Array.Empty<Task>();
    private static string _bootPath = "", _bootText = "";
    private static readonly HashSet<string> Completed = new(StringComparer.Ordinal);

    internal static void Start()
    {
        UnityEngine.Application.quitting -= ApplicationQuitting;
        _root = UnityEngine.Application.persistentDataPath;
        _diagnosticPath = Path.Combine(_root,"Logs","BridgeBuilder.Deletion.Shutdown.log");
        Completed.Clear();
        _normalShutdown = false;
        _shutdownTasks = Array.Empty<Task>();
        try
        {
            _manager = GameManager.instance;
            _database = AssetDatabase.global;
            _tasks = Colossal.TaskManager.instance;
            _bootPath = Path.Combine(UnityEngine.Application.dataPath,"boot.config");
            _bootText = File.ReadAllText(_bootPath);
            UnityEngine.Application.quitting += ApplicationQuitting;
        }
        catch(Exception e) { Record(true,"Shutdown observation unavailable; deletion records retained: "+e); }
    }

    internal static void LogicallyCompleted(string owner) => Completed.Add(owner);
    internal static void WorldChanging() { if(!_normalShutdown) Completed.Clear(); }
    internal static void DisposingMod()
    {
        _normalShutdown = ReferenceEquals(GameManager.instance,_manager)
            && _manager?.state == GameManager.State.Quitting;
        if(!_normalShutdown)
        {
            UnityEngine.Application.quitting -= ApplicationQuitting;
            return;
        }
        try
        {
            _shutdownTasks = _tasks!.taskRegistry.Values.ToArray();
            Record(false,"Normal shutdown committed; final Unity quit event retained; logicalOwners="+Completed.Count);
        }
        catch(Exception e)
        {
            _normalShutdown=false;
            Record(true,"Cannot capture native shutdown work; intent retained: "+e);
        }
    }

    private static void ApplicationQuitting()
    {
        UnityEngine.Application.quitting -= ApplicationQuitting;
        bool committed = _normalShutdown;
        _normalShutdown = false;
        try
        {
            // Never use AssetDatabase.global here: its getter creates a new database after disposal.
            bool terminal = committed && _manager?.state == GameManager.State.Terminated;
            bool databaseDisposed = !AssetDatabase.exists && _database != null && _database.databases.Count==0;
            bool drained = _tasks != null && _tasks.taskQueue.Complete().Status==TaskStatus.RanToCompletion
                && _tasks.taskQueue.count==0 && _tasks.taskRegistry.Count==0
                && _shutdownTasks.All(t=>t.Status==TaskStatus.RanToCompletion);
            bool worldsDisposed = true;
            // Native NoAllocReadOnlyCollection rejects IEnumerable/LINQ enumeration.
            foreach(var world in Unity.Entities.World.All)
                if(world.IsCreated) { worldsDisposed=false; break; }
            bool single = _bootText.Split('\n').Any(l=>l.Trim()=="single-instance=")
                && File.ReadAllText(_bootPath)==_bootText;
            using var current = Process.GetCurrentProcess();
            bool competitor = current.ProcessName != "Cities2";
            foreach(var process in Process.GetProcessesByName("Cities2"))
            {
                using(process) if(process.Id!=current.Id && !process.HasExited) competitor=true;
            }
            Record(false,"Final Unity quit event; terminal="+terminal+"; databaseDisposed="+databaseDisposed
                +"; nativeDrain="+drained+"; worldsDisposed="+worldsDisposed
                +"; nativeSingleInstance="+single+"; competingInstance="+competitor);
            foreach(var owner in BridgeDeletionIntent.Owners(_root))
            {
                if(!Eligible(Completed.Contains(owner),terminal && databaseDisposed && drained,worldsDisposed,single,competitor))
                {
                    Record(true,"Shutdown cleanup pending: UUID="+owner+"; logicalComplete="+Completed.Contains(owner));
                    continue;
                }
                try
                {
                    BridgeDeletionIntent.Cleanup(_root,owner);
                    try { BridgeLegacyRemoval.Finish(owner); }
                    catch(Exception e) { Record(true,"Physical cleanup completed but legacy ticket remains; UUID="+owner+"; "+e); }
                    Record(false,"Verified normal-shutdown physical cleanup: "+owner);
                }
                catch(Exception e) { Record(true,"Shutdown cleanup failed; UUID="+owner+"; durable intent retained: "+e); }
            }
        }
        catch(Exception e) { Record(true,"Shutdown cleanup observation failed; durable intent retained: "+e); }
        finally
        {
            Completed.Clear();
            _shutdownTasks=Array.Empty<Task>();
            _manager=null; _database=null; _tasks=null;
        }
    }
    internal static bool Eligible(bool logicalComplete,bool drained,bool worldsDisposed,bool nativeSingleInstance,bool competitor)
        => logicalComplete && drained && worldsDisposed && nativeSingleInstance && !competitor;
    private static void Record(bool error,string message)
    {
        // Colossal logging has already been released before the final Unity quit event.
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_diagnosticPath)!);
            File.AppendAllText(_diagnosticPath,DateTimeOffset.Now.ToString("o")+" ["+(error?"ERROR":"INFO")+"] "+message+Environment.NewLine);
        }
        catch { } // A diagnostic failure must never escape process shutdown.
    }
}
