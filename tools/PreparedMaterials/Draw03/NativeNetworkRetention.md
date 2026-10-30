# Draw03 native network material retention

## Representation and source evidence

The five fixed-approach families already enter `Draw03ApproachAssembly` as native
`NetPiecePrefab` roots. Their two lower LOD children are `RenderPrefab` instances,
but are still rendered by the parent network graph. `Draw03ApproachLayout` binds
the generated pieces into native sections, and `MovableRoadAssets` carries the
network fields, components and rewritten LOD links into the owned graph. This is
different from the skinned main mechanism and ordinary object pillars.

`Draw03NativeMaterials.IsNativeNetwork` identifies the fifteen exact
render/mesh-zero/Surface-CID combinations from those reconstructed source graphs.
The shared caller must additionally establish native network graph capability and
validate the actual Surface template. A name or source CID alone cannot authorize
network sampling on an object. The root's capability applies to all its LODs;
testing each LOD's CLR type independently would incorrectly convert the lower LODs.

`VerifyNativeNetwork.py` rechecks the original archived Surface hashes, recorded
native NPZ hashes, reconstruction geometry and Surface identities, UV values and
native shader evidence. The fifteen streams contain 12,771 original domains. All
used U coordinates lie within [0,1]; every used V is strictly inside (-1,0), and
the recorded physical chart traversal has no periodic-reset seams. Their UV0
mapping is finite authoring in both directions, not a positive-V length rate.

The inspected shader 581 vertex program selects `frac(V)` for these negative-V
vertices. Retaining the original network material and encoded UVs keeps that
branch and its worldspace texture family, mask-blue mixing and original mip
behavior. There is no need to prove that a template-0 descriptor can reproduce
the separate worldspace layer. The 17-texel mask expansion from the earlier
conversion investigation remains insufficient evidence for such a conversion.

## Scope and integration

In a verified native network graph, retain the original generated structure model
and original Surface. The model already contains the authorized measured position
transformation; raw UVs and other carried channels remain those of the prototype.
Do not apply the object-path density frame, create additional charts, replace the
Surface descriptor or bake texture pixels. Native worldspace sampling retains its
physical mapping; the evidenced finite UV0 pattern may stretch with the geometry.

The baseline actual-plan observer attributes all 22,766 unresolved Draw03 uses to
these `Draw03ApproachGeometry` streams. That attribution is an implementation target,
not a new observer pass or a rendered-game acceptance result. The pure identity
predicate leaves object mechanisms, pillars, unknown renders, other mesh slots and
mismatched source CIDs outside this route. Existing object conversion remains
separate; no whole-domain UV flag or geometry recipe was changed.

The shared integration must verify the graph capability, exact original Surface
identity and raw native upload through every LOD. Preview and saved creation use
the same graph writer, whose existing owned-prefab persistence saves rewritten LOD
links and original built-in references. Actual near/far appearance, worldspace
layer rendering and manager-independent cold loading remain human checks.

## Isolated native integration evidence

`NativeNetworkCheck.Editor.cs` runs in the Draw03-owned Unity project. It loads
the compiled mod and real game managed libraries; it never constructs a bridge,
registers a prefab in the game, or opens a city. Fifteen archived original Surface
headers and fifteen prototype mesh position/index/UV0.xy streams exercise the
actual `Draw03ApproachGeometry.Create`, structure builder, `Append`, and
`Model.ToUnityMesh` methods. Three length deltas and two channel variants give
90 cases and 153,462 uploaded vertices. Ninety negative controls reject object
context, wrong slot/CID/render/template, and non-built-in provenance. Separate
graph tests reject object roots, road-bearing LODs, and skinned LODs.

The second channel variant supplies explicit UV0.zw, COLOR and bone sentinels.
Those sentinels prove transfer integrity, not their presence in the original
prototype. UV0.xy, positions and indices come from the original extracted mesh.
Tests assert the exact model and Surface objects survive Append and inspect the
uploaded vertex streams, including bone bytes. Archived Surface headers are read
by the real Surface implementation. Built-in provenance is controlled by an
uninitialized `AssetDatabase<Game>` identity assigned to the fixture; no global
database, resource registry, or game loading is claimed by this setup.

The real native `AddComponentFrom` method separately copies `NetPieceTiling`
with `m_DisableTextureTiling=true` and `LodProperties` with both child references.
This demonstrates native component-copy behavior, not end-to-end serialization.

### Source-traced graph and persistence path

1. `Draw03BridgeGenerator` invokes `Draw03ApproachAssembly`, which builds the
   five native root graphs and all LOD levels through `CreateMovableRenderGraph`.
   `Draw03ApproachLayout` binds the resulting pieces to native network sections.
2. `TowerFactory.Movable` calls `MovableRoadAssets.TryCreate` and records created
   prefabs. The writer carries root graph capability to every LOD, copies native
   NetPiece fields and components, and remaps each `LodProperties.m_LodMeshes`
   reference to the corresponding owned prefab in the same output group.
3. Its preview branch captures models and original Surface references in
   `PreviewGeometry`. The saved branch creates, saves and unloads Geometry assets.
4. `BridgeGenerationSystem.Composition` includes `towers.Created` in owned nodes,
   verifies ownership, then calls `BridgePrefabPersistence.Save` and
   `BridgeDependencyPersistence.Save` before native publication. Preview lifetime
   adopts these nodes through `preview.Adopt` instead.

These are inspected source dependencies, not executed save/load evidence.
Full `TryCreate`, native geometry registration, serialization, lower-LOD visual
switching and manager-independent cold loading remain manual acceptance tasks.
The standalone Editor has no native AssetDatabase resources; fabricating these
resources would not constitute proof of real persistence. No such bypass or
automated bridge generation was attempted.

### Commands

Run with the Python environment containing NumPy:

```powershell
python tools/PreparedMaterials/Draw03/VerifyNativeNetwork.py --output artifacts/material-resume/Draw03/native-network-evidence.json
```

The output records fifteen source streams, 12,771 source domains and 22,766
baseline unresolved uses, together with source, predicate and shader hashes.
This change adds no UI text or localization keys and changes no texture payload,
geometry, bone, selected-road layout or saved bridge identity.
