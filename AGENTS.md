# Repository Agent Instructions

## Scope

This file applies to the entire repository. It contains the repository-level rules that Codex must
load before doing any work. More specific `AGENTS.md` files may add instructions for their directory,
but they may not weaken or replace this file or the project contract.

The complete, pre-existing project contract is preserved in
[`docs/agent-contract/PROJECT_CONTRACT.md`](docs/agent-contract/PROJECT_CONTRACT.md). That document is
normative, not historical background and not optional reading. The summaries below provide routing
and fast checks; they do not replace any detailed requirement, example, exception or failure mode in
the contract. If a summary could be read less strictly than the contract, follow the contract.

## Mandatory reading

Read the required documents completely before the first edit. Do not rely on memory or on a previous
session.

| Work being performed | Required instructions |
| --- | --- |
| Any bridge, mesh, prefab, tower, cable, LOD, geometry, export or generation change | This file, the complete project contract, and the nearest directory-level `AGENTS.md` |
| Runtime code under `src/BridgePrefabGenerator` | This file, the complete project contract, and [`src/BridgePrefabGenerator/AGENTS.md`](src/BridgePrefabGenerator/AGENTS.md) |
| Geometry identification or metadata generation under `tools/GeometryMetaprogram` | This file, contract sections 1–9 and 13, and [`tools/GeometryMetaprogram/AGENTS.md`](tools/GeometryMetaprogram/AGENTS.md) |
| Diagnosis or visual validation | Contract sections 1 and 6–9, plus the nearest directory-level `AGENTS.md` |
| Double-deck bridge work | The complete project contract, especially section 11 |
| Git, branching, commit or integration work | Contract section 12 before changing files or branches |
| Build, install or generated-asset cleanup after a source update | Contract sections 6 and 13 |
| Instruction-document maintenance only | This file and every instruction file being changed |

When a task spans more than one row, combine their reading requirements. When scope is uncertain,
read the complete project contract.

## Non-negotiable project rules

The numbered references below point to the corresponding sections in the complete project contract.

1. Every generated bridge follows a real archetype of the same type. Preserve the complete prefab,
   component, placement, mesh-part, spawn and pillar behavior unless the deliberate difference is
   recorded at the point of change. See contract section 1.
2. Archetype parameters are measured once and hardcoded. Runtime generation must not depend on the
   archetype prefab being installed. Existing input components are carried across whole except for
   the exact modification requested. See contract section 2.
3. A generated bridge may differ from its archetype only in generated geometry and the user-selected
   road structure. See contract section 3.
4. Towers and cables are derived geometry owned by the generated bridge; they are never references to
   shared archetype geometry. Each generated bridge receives its own bridge-prefixed structure
   prefabs. See contract section 4.
5. A generated tower differs from its archetype in width alone. Preserve all other measured
   relationships and prefab behavior. See contract section 5.
6. Compilation, synthetic numbers and offline tests cannot prove a visual fix. Agents must not run
   unit tests for visual component-generation work. Inspect the real prototype and generated geometry,
   read export diagnostics, and finish with in-game near- and far-view checks. Every visual change must
   update the highest-detail mesh and every LOD together. See contract section 6.
7. Diagnose by dumping the archetype and generated object, normalizing the data and diffing them. Read
   the game IL when field behavior is unclear. Do not substitute visual guesses for evidence. See
   contract section 7.
8. Stretch versus translation is decided only by whether the authored part reaches or crosses
   `x = 0`. This rule has priority over every family special case, heuristic and agent-authored rule,
   and every attempted override must be refused. Runtime spatial comparisons may compare `x`, `y` or
   `z` with zero using equality or inequalities; they may not use a fixed non-zero coordinate to infer
   geometry. Non-zero geometric inference is allowed only in metaprogramming, whose reviewed result is
   committed as immutable, hardcoded runtime data. Runtime geometry guessing is forbidden. See
   contract section 8.
9. The suspension family is the reference checklist; every family must preserve the equivalent
   archetype relationships rather than inherit suspension-specific values. See contract section 9.
10. Unsupported generation is refused with a reason. It must not silently fall back to an unrelated
    archetype or partial bridge. See contract section 10.
11. A double-deck bridge is built only from its double-deck archetype. Preserve its main/auxiliary net
    roles, positions, inversion behavior and fixed deck separation. See contract section 11.
12. A bridge-specific change is made only on the exact all-English `bridge/<style-slug>` branch listed
    in contract section 12. Check `git branch --show-current` before reading code with intent to edit.
    Shared infrastructure is developed on `dev`; it is not disguised as a single-bridge change.
13. After every source-code update, invoke the exact `Cities2.exe` kill command even if it is a no-op,
    verify the process is absent, then remove and verify removal of every bridge and generated artifact
    created by the mod. Building or installing does not satisfy these requirements. See contract
    section 13.

## Geometry invariants that must remain visible at repository scope

- A part that reaches or crosses `x = 0` stretches about the centre according to its own span.
- A side part that does not reach `x = 0` translates rigidly by half the width delta. It must not be
  thickened, thinned or distorted.
- The base is the structure immediately adjacent to and directly below the road deck. It is not a
  pier footing, pillar foot plate, tower foundation or a prefab selected by a name containing `Base`.
  A candidate that does not reach or cross `x = 0` is not the base.
- A base-width adjustment is a coordinate mapping, never direct width scaling:

      x -> x + sign(x) * delta

- Bridge-pier columns translate; they do not stretch.
- The highest-detail archetype identifies each authored part once. Every LOD must receive the same
  logical classification and compatible transformation.
- Code under `src/BridgePrefabGenerator` must not contain an explicit `throw` statement. Return an
  explicit failure result, record the reason and stop the affected prefab before publishing geometry.
  Continue catching exceptions raised by the game and third-party APIs.

## Workflow

1. Confirm the current branch and cleanly separate unrelated user changes.
2. Read the instruction files required by the table above.
3. Inspect the archetype and current generated output before changing transformation code.
4. Make only the requested, branch-valid change. Keep runtime inference and metaprogramming separate.
5. Apply the change consistently to the full-detail mesh and all LODs.
6. After any source-code edit, perform the mandatory process stop and ownership-scoped generated-bridge
   cleanup from contract section 13.
7. Compile when appropriate, but report compilation only as a loadability check.
8. Do not call a visual issue fixed until the in-game near/far result has been observed.

## Maintaining these instructions

- Preserve every existing normative rule. Reorganization may move text, but it may not delete, weaken
  or silently supersede a requirement.
- Keep repository-wide rules in this root file. Put path-specific additions in the nearest directory
  `AGENTS.md`; a nested file is additive unless it explicitly tightens a rule for that scope.
- Keep this automatically discovered instruction chain below Codex's default 32 KiB combined limit.
  Store long explanations and failure history in the normative project contract and route agents to
  the exact material they must read.
- Use a single H1 title, ordered H2 sections, CommonMark tables/lists, fenced or indented code only for
  literal commands and formulas, and repository-relative links.
- Do not add `AGENTS.override.md` as a permanent repository rule. Overrides take precedence and can
  silently hide the regular file in the same directory.
- When changing a rule, update this routing summary, the authoritative contract and any applicable
  directory-level file together.
