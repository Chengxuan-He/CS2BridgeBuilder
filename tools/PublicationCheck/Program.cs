using BridgeBuilder.Systems;
using CS2Mods.Shared.Conversion;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using Game.UI.InGame;
using Unity.Entities;

// Publication/menu regression only. No mesh generation, game launch, disk assets or real save data.
var world = new World();
var prefabs = world.GetOrCreateSystemManaged<PrefabSystem>();
var publisher = world.GetOrCreateSystemManaged<BridgePublicationSystem>();
var toolbar = world.GetOrCreateSystemManaged<ToolbarUISystem>();
var category = new UIGroupPrefab { name = "Roads" };
prefabs.AddOrUpdatePrefab(category);
prefabs.NativePass();
var original = Road("original road");
prefabs.AddOrUpdatePrefab(original);
prefabs.NativePass();
var first = Road("r-first");
var dependency = new PrefabBase { name = "bridge dependency" };
var report = new ExportReport();
var completions = 0;
var success = false;
Check(publisher.Publish(new[] { Node(first), Node(dependency, false), Node(dependency, false) }, report,
    ready => { completions++; success = ready; }), "queue first bridge");
Check(publisher.IsPending && completions == 0 && Entries() == 1, "registration must not initialize or activate");
Check(prefabs.AddCalls.TakeLast(2).SequenceEqual(new[] { dependency, first }), "distinct dependencies before root");
Check(!publisher.Publish(new[] { Node(Road("overlap")) }, new ExportReport(), _ => { }), "reject overlapping publication");
prefabs.NativePass();
publisher.Update();
Check(success && completions == 1 && Entries() == 2, "complete once after native pipeline");
publisher.Update();
Check(completions == 1 && Entries() == 2, "repeated tick must not append UI entries");
Check(toolbar.AssetRefreshes == 1 && toolbar.CategoryRefreshes == 1, "refresh native cached list");

var second = Road("r-second");
Check(publisher.Publish(new[] { Node(second) }, new ExportReport(), ready => { Check(ready, "second ready"); completions++; }), "queue second");
prefabs.NativePass();
publisher.Update();
Check(Entries() == 3 && completions == 2, "one item each after consecutive creations");
Check(prefabs.Initializations[first] == 1 && prefabs.Initializations[second] == 1
    && prefabs.Initializations[original] == 1, "source road is not reinitialized");

var oldFirst = prefabs.EntityOf(first);
Check(publisher.Publish(new[] { Node(first) }, new ExportReport(), ready => { Check(ready, "updated ready"); completions++; }), "queue existing update");
publisher.Update();
Check(publisher.IsPending && completions == 2 && prefabs.EntityOf(first) == oldFirst, "wait for outer replacement pass");
prefabs.ApplyQueuedUpdates();
prefabs.NativePass();
publisher.Update();
Check(completions == 3 && Entries() == 3, "replace without duplicating menu or callback");

var updateFailedReport = new ExportReport();
var updateFailedReady = true;
publisher.Publish(new[] { Node(first) }, updateFailedReport, ready => updateFailedReady = ready);
publisher.Update();
publisher.Update(); // Simulate the outer update failing to replace the old entity.
Check(!publisher.IsPending && !updateFailedReady && updateFailedReport.Failures.Count == 1,
    "failed replacement must not block the request queue forever");
prefabs.ApplyQueuedUpdates();
prefabs.NativePass();

var broken = Road("missing network data");
var brokenReport = new ExportReport();
var brokenReady = true;
publisher.Publish(new[] { Node(broken) }, brokenReport, ready => brokenReady = ready);
// Simulate the native network initialization failure: entity exists, instance archetypes do not.
publisher.Update();
Check(!brokenReady && brokenReport.Failures.Count == 1, "never activate a half-initialized road");
prefabs.NativePass();

var duplicate = Road("duplicate menu member");
var duplicateReport = new ExportReport();
var duplicateReady = true;
publisher.Publish(new[] { Node(duplicate) }, duplicateReport, ready => duplicateReady = ready);
prefabs.NativePass();
var buffer = world.EntityManager.GetBuffer<UIGroupElement>(prefabs.EntityOf(category));
buffer.Add(new UIGroupElement { m_Prefab = prefabs.EntityOf(duplicate) });
publisher.Update();
Check(!duplicateReady && duplicateReport.Failures.Any(text => text.Contains("2 entries")), "reject duplicate category membership");

var rejected = Road("rejected registration");
prefabs.Reject = rejected;
var rejectedCallback = false;
Check(!publisher.Publish(new[] { Node(rejected) }, new ExportReport(), _ => rejectedCallback = true), "registration failure returned");
publisher.Update();
Check(!rejectedCallback && !publisher.IsPending, "rejected publication has no success callback");

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var modSource = File.ReadAllText(Path.Combine(root, "src/BridgeBuilder/Mod.cs"));
var generationSource = File.ReadAllText(Path.Combine(root, "src/BridgeBuilder/Systems/BridgeGenerationSystem.cs"));
Check(modSource.Contains("UpdateBefore<BridgeGenerationSystem>(SystemUpdatePhase.PrefabUpdate)")
    && modSource.Contains("UpdateAfter<BridgePublicationSystem>(SystemUpdatePhase.PrefabUpdate)"), "native scheduler owns initialization");
Check(!generationSource.Contains("WorldRegistration.Publish("), "do not re-enter synchronous shared publisher");
Console.WriteLine("PASS: publication ordering, one-time menu membership, consecutive creates, deferred updates and failure gating.");

NetPrefab Road(string name) => new()
{
    name = name,
    m_Sections = new object[] { new() },
    components = new() { new UIObject { m_Group = category } }
};
PrefabCloneNode Node(PrefabBase target, bool root = true) => new(target, root, true);
int Entries() => world.EntityManager.GetBuffer<UIGroupElement>(prefabs.EntityOf(category)).Length;
static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
}
