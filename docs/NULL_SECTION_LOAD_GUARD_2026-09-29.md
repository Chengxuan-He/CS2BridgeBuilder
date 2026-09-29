# Null section registration guard

The reported `ArgumentNullException(key)` occurs in native `NetInitializeSystem.AddSections`:
it passes a null `NetSectionInfo.m_Section` to `PrefabSystem.GetEntity`. A map-complete
cleanup cannot prevent this earlier `PrefabUpdate` failure. Native IL also calls AddSections
for OverheadNetSections and UndergroundNetSections. Null section/auxiliary arrays can fail
even earlier, during AddPrefab's IsUnlockable/GetDependencies traversal.

The guard prefixes AddPrefab and UpdatePrefab, before native dependency enumeration or ECS
registration. Only writable Bridge Builder root/deck names (`b{uuid}`, `_Upper`, `_Lower`)
are intercepted. Main, overhead, underground and recursively carried auxiliary networks are
checked for null arrays, null entries and null prefab references. Optional empty arrays remain
valid; cycles terminate by object identity. No missing-registration check is made while
dependencies are still loading, and no source prefab, array, material or shared asset is changed.

Rejected objects are retained across map preload in the in-memory quarantine set, not entered
into PrefabSystem. Post-load cleanup enumerates them as well as registered objects, revalidates
current structure, and requires stable evidence on separate engine frames before deletion.
Recovered unregistered networks are offered to native registration again. Saved references to
rejected IDs can resolve as obsolete; existing ownership-scoped PostTool network retirement
remains in charge. Backing files are removed only after placed references have retired. The
guard does not destroy entities, remove registered prefab indices or suppress native exceptions
globally. Already registered networks rejected on UpdatePrefab retain their existing ECS data
until safe cleanup; this is not a general repair of arbitrary external in-place mutations.

Harmony 2.2.2's self-contained Mono assembly and license are now included in build, install and
release staging. No external Harmony mod installation is required. Startup logs the hook
installation or an explicit critical failure; the local game still needs to verify hook execution.

Offline checks: 19 array/auxiliary graph cases, including recovered references and cycles; 23
existing instance-removal/cleanup-confirmation checks. Release build compiled against installed
game assemblies. These do not prove native game initialization or placed-network deletion.

Deployment cleanup backup: `C:\Users\admin\Downloads\BridgeBuilder-null-section-fix-20260929`.
The pre-existing corruption laboratory and its separate ZIP backup remain intact. Generated
asset cleanup removed 304 imported directories, 322 geometry files and 2 state files; 9
RBExportDep directories were preserved. Save files were not edited.

Human acceptance: restore the corruption fixtures only into a test setup, launch the game, and
check for `Bridge prefab load guard installed` followed by per-prefab `Quarantined bridge`
warnings, no AddSections critical, and completion of the safe cleanup. Check a healthy bridge
control and map with actual placed bridge instances separately. Identifier collisions, arbitrary
piece/geometry corruption and unrelated mods' malformed prefabs are not claimed fixed by this guard.
