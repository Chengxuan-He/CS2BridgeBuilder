# Lift04 task 65 generation guard audit

Scope: all Lift04 runtime C# files and Lift04 GeometryMetaprogram producers. No runtime or producer changes are needed in this style. Mapping, topology, source data and material bindings remain unchanged.

## Retained guards

- Recorded source counts, triangle indices, topology hashes, LOD enumerations and BBN schema checks protect array access and measured source identity; they do not evaluate generated structural quality.
- Nonfinite delta checks preserve representable arithmetic errors. They do not reject any finite zero or negative value.
- Missing native render graphs, section/object references and render construction results remain real dependency/native failures. SourceRenders rethrows after cleanup.
- NetworkData forwards shared MovableSectionConversion.TryConvert errors. The shared implementation requires the independent task 65 policy audit; do not swallow real conversion failures here.
- UvCharts missing lookup returns an empty chart list; UvSampling missing lookup returns zero. These are lookup outcomes, not generation cancellation.

## Complete throw inventory

| File:line | Retained statement |
| --- | --- |
| Lift04BridgeGenerator.cs:33 | return Lift04RenderAssembly.CreateMechanism(factory, owner + " Mechanism", levels, halfDelta) ?? throw new InvalidOperationException("Lift04 moving render graph creation failed."); |
| Lift04BridgeGenerator.cs:38 | var column = Lift04SupportAssembly.Create(factory, target.name + " Column", roots[Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values.I32[0]], false, halfDelta) ?? throw new InvalidOperationException("Lift04 support render graph creation failed."); |
| Lift04BridgeGenerator.cs:39 | var footing = Lift04SupportAssembly.Create(factory, target.name + " Footing", roots[Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values.I32[1]], true, halfDelta) ?? throw new InvalidOperationException("Lift04 footing render graph creation failed."); |
| Lift04ColumnCapitals.g.cs:16 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04ColumnCapitals.g.cs:18 | throw new InvalidOperationException("Lift04 column capitals requires its recorded native topology."); |
| Lift04Connectors.g.cs:15 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04Connectors.g.cs:17 | throw new InvalidOperationException("Lift04 connectors require their recorded topology."); |
| Lift04Crossbeams.g.cs:15 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04Crossbeams.g.cs:17 | throw new InvalidOperationException("Lift04 crossbeams require their recorded topology."); |
| Lift04DeckBase.g.cs:16 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04DeckBase.g.cs:18 | throw new InvalidOperationException("Lift04 deck base requires its recorded native topology."); |
| Lift04FarShoulders.g.cs:13 | throw new InvalidOperationException("Lift04 far shoulders require the recorded edge-only structure plan."); |
| Lift04FarShoulders.g.cs:33 | throw new InvalidOperationException("Lift04 far road source topology changed."); |
| Lift04FixedBodies.g.cs:16 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04FixedBodies.g.cs:18 | throw new InvalidOperationException("Lift04 fixed bodies requires its recorded native topology."); |
| Lift04Glass.g.cs:16 | throw new InvalidOperationException("Lift04 glass requires its recorded mesh-1 layout."); |
| Lift04Machinery.g.cs:15 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04Machinery.g.cs:17 | throw new InvalidOperationException("Lift04 machinery require their recorded topology."); |
| Lift04MechanismData.g.cs:46 | throw new ArgumentException("Lift04 requires its main, pillar and pillar-base render graphs."); |
| Lift04MechanismData.g.cs:777 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04MovingEndFrames.g.cs:16 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04MovingEndFrames.g.cs:18 | throw new InvalidOperationException("Lift04 moving end frames require their measured layout."); |
| Lift04MovingSideFrames.g.cs:15 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04MovingSideFrames.g.cs:17 | throw new InvalidOperationException("Lift04 detailed moving side frames requires its recorded native topology."); |
| Lift04NearAppearance.cs:18 | throw new InvalidOperationException("Invalid Lift04 continuous appearance schema or LOD."); |
| Lift04NetworkAssembly.cs:14 | throw new InvalidOperationException("Lift04 network assembly requires its owned mechanism."); |
| Lift04NetworkAssembly.cs:19 | throw new InvalidOperationException("Lift04 requires the built-in subway pillar placeholder."); |
| Lift04NetworkData.g.cs:21 | throw new InvalidOperationException("Lift04 requires a measured role for each source occurrence."); |
| Lift04NetworkData.g.cs:28 | _ => throw new InvalidOperationException("Unknown Lift04 section role: " + roles[i])}; |
| Lift04NetworkData.g.cs:30 | throw new InvalidOperationException(error); |
| Lift04ObjectAssembly.cs:19 | throw new InvalidOperationException("Lift04 requires its original tower attachment sources."); |
| Lift04RenderAssembly.cs:17 | throw new ArgumentException("Lift04 mechanism requires all three native road LODs."); |
| Lift04RenderAssembly.cs:22 | throw new InvalidOperationException($"Lift04 LOD {lod} requires its reconstructed native source."); |
| Lift04RenderData.g.cs:25 | throw new ArgumentException("Lift04 Mechanism requires 3 prepared render levels."); |
| Lift04RenderData.g.cs:52 | throw new ArgumentException("Lift04 Column requires 3 prepared render levels."); |
| Lift04RenderData.g.cs:79 | throw new ArgumentException("Lift04 Footing requires 3 prepared render levels."); |
| Lift04RenderData.g.cs:107 | throw new ArgumentOutOfRangeException(nameof(part)); |
| Lift04RigidParts.g.cs:17 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04RigidParts.g.cs:19 | throw new InvalidOperationException("Lift04 side-part source layout changed."); |
| Lift04RoadTops.g.cs:14 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04RoadTops.g.cs:16 | throw new InvalidOperationException("Lift04 detailed road-top layout changed."); |
| Lift04RoadTops.g.cs:21 | throw new InvalidOperationException("Lift04 detailed road-top topology changed."); |
| Lift04SectionRoles.cs:15 | throw new InvalidOperationException("Lift04 selected network contains a missing section."); |
| Lift04SourceRenders.cs:48 | throw; |
| Lift04SupportAssembly.cs:14 | throw new InvalidOperationException("Lift04 supports require their three recorded native LODs."); |
| Lift04SupportAssembly.cs:27 | throw new InvalidOperationException("Lift04 supports require their reconstructed native render inputs."); |
| Lift04SupportAssembly.cs:33 | throw new InvalidOperationException("Lift04 supports require their recorded single native surface slot."); |
| Lift04SupportGeometry.g.cs:19 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04SupportGeometry.g.cs:21 | throw new InvalidOperationException("Lift04 support requires its recorded topology."); |
| Lift04TowerFrames.g.cs:16 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04TowerFrames.g.cs:18 | throw new InvalidOperationException("Lift04 tower frames requires its recorded native topology."); |
| Lift04TrackRemoval.g.cs:14 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04TrackRemoval.g.cs:16 | throw new InvalidOperationException("Lift04 native track source layout changed."); |
| Lift04TrackRemoval.g.cs:23 | throw new InvalidOperationException("Lift04 native track source topology changed."); |
| Lift04UpperDetails.g.cs:15 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04UpperDetails.g.cs:17 | throw new InvalidOperationException("Lift04 upper details require their measured layout."); |
| Lift04UpperShells.g.cs:16 | _ => throw new ArgumentOutOfRangeException(nameof(lod))}; |
| Lift04UpperShells.g.cs:18 | throw new InvalidOperationException("Lift04 upper tower shells requires its recorded native topology."); |
| Lift04UvCharts.cs:36 | if (i[0] != 1) throw new InvalidOperationException("Invalid Lift04 UV chart schema."); |
| Lift04UvSampling.cs:29 | throw new InvalidOperationException("Lift04 UV sampling domain mismatch."); |
| Lift04UvSampling.cs:40 | throw new InvalidOperationException("Invalid Lift04 UV sampling schema."); |
| Lift04UvSampling.cs:51 | throw new InvalidOperationException("Invalid Lift04 UV sampling flag."); |
| Lift04UvSampling.cs:56 | throw new InvalidOperationException("Invalid Lift04 UV sampling length."); |

## Verification

Static inspection found
63
 throw statements and
1
 return-false statements in
39
 runtime files. All throw conditions were reviewed against their source conditions. No structural quality, clearance, signed-width or positive-span rejection was found in Lift04.

Lift04 producer emission retains only source-layout, LOD and nonfinite-input errors. Offline orientation assertions in Crossbeams, Supports and UpperDetails are measurement checks, not runtime request gates. No producer regeneration was performed.

No new user-visible strings: existing localization is unchanged. Full integration/build and runtime signed-span tests are shared-task responsibilities; this style-only static audit does not claim a game execution test. No deployment.
