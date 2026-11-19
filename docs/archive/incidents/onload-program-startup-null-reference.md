# Startup initialization failure investigation

## Observed failure

On 2026-11-18 the game displayed a blue fallback screen with `NULL_REFERENCE_EXCEPTION`.
Windows remained operational. The inspected Game.dll is version 1.6.4f1, SHA-256
`F076DF4B70AA0F21A80345E03A052CA2273BC6754F41DAB7E63D837837A43745`, MVID
`46198354-d4f9-4d23-bf32-7e3c085d8b92`.

The initial installed OnLoad-only catalogue DLL was `32CA0910C8AC6C4EC71EAD9B6BB8A68DFD0B5D5FF24390AA444925905B65A8C4`.
Diagnostic builds `A63FB07A144E81000B2FF06CD6967823E1F7AC966995A5DC78447A72947B8C2F`
and `5E6543926BC54FE7249368E6F5499C197DA614ECAE6BF28F36047596EAD4B279` reproduced
the same `GameManager.Initialize` failure reported at offset `0xa79`.

Both native `SharedSettings.Apply` and `EnableSettingsDependantObjects` completed.
The later-installed state-machine probes received no subsequent entry before failure;
therefore these logs do not reveal the original `assetLoading` task state. Do not infer an
empty or failed task solely from absent kickoff logs. The inspected IL offset lies in a
finally region and has not established the original null operand.

## Bounded checks and limits

An isolated Unity/Mono run loaded all fourteen production graph identities simultaneously,
retaining every Material/Shader and exact queue. Repeated registration, second-library reuse
and foreign collision refusal passed. This does not reproduce the game's complete startup.
A read-only offline check found twelve current ImportedData Surface files, none using a
current custom template identity. No player files were changed.

Read-only extraction of the running Initialize JIT body confirmed the post-settings task-await
sequence. The already-unwound main thread stack no longer retained the initialization frame;
no task value was recovered from it. No process was injected into, suspended or terminated.
Evidence is under `work/onload-crash-20261118`, including `relaunch` and `relaunch2`.

<a id="controlled-isolation-candidate"></a>

## Historical isolation proposal

The investigation proposed omitting only the OnLoad registration call and removing temporary
startup probes. The proposed candidate had no late registration, Surface scan, fallback
registration or persistence change; custom-program operations would intentionally be unavailable.
Ordinary source prerequisites and generated-bridge behavior were to remain unchanged.
The plan called for separate labelled artifacts, restoration of the intended fourteen-entry
OnLoad-only source after building, verified installed-file backups and a manual user launch.
No outcome for that specific proposed isolation candidate was recorded here, so it does not
establish either a repair or attribution of the failure to program registration.

## Current status

The OnLoad-only catalogue and isolation proposal above are historical experiments, not current
operating instructions. The maintained implementation uses the [three-stage loading workflow](../../specs/custom-shader-spec.md#custom-shader-loading-workflow): OnLoad restoration, game-load
completion restoration, and generation/preview preparation on demand. This lifecycle restoration
does not identify the null operand or establish that the initialization failure has been fixed.
