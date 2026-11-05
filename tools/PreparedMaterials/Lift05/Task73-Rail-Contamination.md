# Lift05 pure road periods — candidate 73

Status: frozen candidate, native checks passed; game appearance and freshly generated owner acceptance pending. No deployment performed by the Lift05 worker.

The installed72r2 native appearance route superseded the older clean-strip route. The central prepared periods sampled prototype road atlas rectangles containing rail bands in base color, normal and mask channels. The far central road similarly sampled a rail-bearing atlas strip. This is source-payload evidence, not an inference from the user screenshot. Existing selected-road rails/paint routing is unchanged; this change does not remove rail geometry indiscriminately.

Actual inspected owner: `bbc255c88-da20-497a-8a54-d2d961f54ff7`, selected Road Builder source `r9e92fb4c-b80e-4810-a5c4-292ad94df7dd-76561199197854251`. The owner existed when read. No owner or healthy asset was removed.

## Frozen change

`PrepareCleanRoad.py` authors three small periodic pure road donors from the measured original atlas: fixed dark road 128×256 at (1664,1728), moving light road 64×256 at (64,2600), and far moving road 3×64 at (277,60). Physical parts retain their distinct source appearance. All three PBR channels share coordinates; boundary blending operates on decoded linear color and normal vectors. Full native integer-halving mip chains are stored losslessly. Source masks have no rail metallic channel in the donor and every output mip retains zero mask RGB.

The schema2 RoadProjection BBN changes 59 central domains to units4–6, including their finite terminals. All 64 side domains and partition trees remain identical. Original texel Jacobians are preserved (maximum reconstructed double error below 1e-12); pure units use both-axis Repeat with an explicit object-origin phase. Runtime changes are confined to `Lift05NativeRoadAppearance.cs` and its producer template. Width collapse/reversal handling is retained; no new structure rejection gate is introduced. Geometry, bones and source topology are not edited.

New graph under `artifacts/material-remap/Lift05/rail-contamination/staged-v3`: 26 files, 207677 bytes including sidecars, 3 Surface descriptors and 10 shared Texture payloads. Shared build verified every staged/snapshot/build file SHA identical.

| Unit | Repeat key | Surface CID |
|---|---|---|
|4|3061b3f27e2581a706363c6b7c7b86b5fe6fb0aba3e69c87a1b1f6318967ebec|e4d91db61aaf481c811cfab769900338|
|5|7a355e468b816e195c561191ac1c5931847c874f2d0cbec74482c662ca3fe6f9|f2de4a0ded42bfe8c2c3b9d227ed764d|
|6|240850aa002f85d71ce250f44328a8ab275e0542717942cee71af30bb82f5042|fe13c74092caebea5b93902eef542e4b|

Authoritative recipe is v3. Earlier staged/v2 artifacts are failed historical candidates, not install inputs. The source BC7 enum is 108=sRGB and 109=UNorm; final native output is 4=sRGB color and 8=UNorm normal/mask. The obsolete continuous installer now delegates to this producer to avoid reinstalling contaminated records.

## Evidence

Candidate DLL: `artifacts/material-remap/shared/outputs/build-native-73/BridgeBuilder.dll`, SHA256 `04380B6925F65610CA651A76839A589955A6A9244C283F0C129F3311F710E308`.

- `PrepareCleanRoad.py --check`: deterministic stage versus installed BBN and all graph files PASS; original far VT/MidMips recovery hashes checked.
- `CheckCleanRoad.py`: all9 PBR chains, all mip decoded hashes, format/filter/wrap/aniso, donor mask exclusion, unchanged side records/partition trees and59 central texel Jacobians PASS. Evidence `rail-contamination/complete-mip-validation.json`.
- Both-axis mip0 edge RMS is zero for every channel. All mip edges are reported separately; maximum channel-byte RMS U/V: fixed color .685/.867, normal2.277/1.848, mask.354/.5; moving color.674/.867, normal.396/4.078, mask2.100/2.062; far color0/.867, normal0/.708, mask0/.5. These are measurements, not a universal visual tolerance approval.
- `road73-rerun.json` and log: all27 combinations (3LODs × width deltas0/3/12 × 3poses) PASS actual ForLod, appearance/physical TryBind, Compose, physical rebind, TryBuild and ToUnityMesh. Selected position, normal, color, provenance, native packed attributes and bone ownership checked. Initial `road73` fixture failed solely because it omitted the newly added optional Compose callback in reflection; fixture corrected to pass null, production unchanged.
- `source-face-coverage/native73-face-coverage.log`: 9 actual main Plan dispositions plus6 glass cases; all15 complete Retile and TryBuild/upload cases PASS with full original packed channels and index/position checks.
- `rail-contamination/native-resources73.log`: actual game SurfaceAsset/TextureAsset readers, dependency closure, property maps and native Texture2D upload PASS for3 surfaces/10 textures;789520 decoded full-mip bytes unchanged.

## Limits and next acceptance

The27 road cases use a synthetic selected receiver, while the source coverage tests use actual prototype Plan geometry. They do not prove a newly generated live selected-road owner, GPU filtering quality, cold reload or user visual acceptance. Root coordinates installation and fresh bridge generation; verify ordinary roads have no prototype rail bands at near/mid/far, bridge heads, finite ends and open poses. Also verify a genuinely rail-bearing selected road retains its tracks and paint. The new graph byte count is not a whole-owner dependency size measurement. No full-road bake, custom shader, source resolution reduction or healthy asset cleanup was performed.
