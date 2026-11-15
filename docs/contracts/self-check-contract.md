# Bridge self-check contract

Required companion: [Self Check Spec](../specs/self-check-spec.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="self-check-1-scope-authority-and-implementation-status"></a>

## Scope, authority and implementation status

All migration-first and supplementation obligations below apply after the explicit obsolete custom-program classification; positively classified legacy custom-program owners enter retirement instead.

This contract and its [paired specification](../specs/self-check-spec.md) together are the single topic authority for automatic and manual bridge self-check, historical
conversion, inspection recovery and its result UI. Generation, native persistence and the shared
file-layout transactions remain in the [generation contract](reading-map.md#bridge-generation-contract) and
[persistence policy](../specs/persistence-spec.md#persistence-6-approved-owner-directory-layout). Explicit player
deletion remains governed by [bridge deletion](reading-map.md#bridge-deletion).

The normal-completion, permission-exception and automatic restart requirements below are approved specifications,
not a statement that the current runtime implements them or has passed acceptance. Historical test
records establish only their recorded behavior. Consolidating these documents changes no runtime code.
The [trusted internal execution rule](../constitution.md#repository-trusted-internal-execution) remains unchanged:
exception boundaries do not authorize internal result validators.

<a id="self-check-startup-filesystem-permission-prerequisite"></a>

## Startup filesystem permission prerequisite

Before the main inspection flow, perform one external-IO permission probe per required bridge
working root at mod startup. Derive those roots from the existing persistence/output configuration,
not a single assumed player path. Use only uniquely named temporary files created by this probe:
write, read, rename/move and delete them. Clean up probe-owned leftovers; do not edit player bridge
contents, source mods or ACLs. Permission denial raises the explicit required-filesystem-permission
exception. Diagnose other IO failures as their actual causes, never as invented permission denial
or successful access. This external IO prerequisite does not authorize internal-result validators.

The probe may run before native assets are ready; it does not advance inspection before native
readiness. Run it once per startup, not each frame or owner. A root probe cannot establish access to
every child file with different ACLs: actual owner/source read/write/move/delete operations still
must propagate required permission failures. The optional recovery backup destination is excluded
from the startup gate because its denied access has the separately authorized deletion fallback.

<a id="self-check-post-load-inspection-and-recovery-prerequisites"></a>

## Post-load inspection and recovery prerequisites

Run self-check only after native loading, mod initialization, PDX database caching and active asset
batches complete at a ready main menu. Observe current state as well as completion events so late
mod loading cannot miss inspection. Include cached user PrefabAssets that failed registration,
not only PrefabSystem's registered list. Do not force-load, republish, repair live references,
reject registration or quarantine assets to perform inspection.

Inspect required live references read-only. When no cached prefab instance exists, a targeted
serialized inspection is permitted. For a required null slot, read its exact original serialized
CID and check whether the dependency already has a cached instance. If it does, recursively copy
permitted unchanged dependency sources under the owning bridge UUID. Keep live references unchanged
and require restart; never serialize a still-null live reference over the recovered disk data.

Follow [section 3](self-check-contract.md#self-check-3-additive-migration-and-owner-scoped-recovery) below to complete missing fields and dependencies while preserving UUID/CID and valid
values. Do not invent historical recipe data. Healthy current assets remain unchanged. Unavailable or unresolved required content must enter the owner repair/recovery flow; explicit nulls without recoverable CIDs, failed registration, unknown required
serialization, or migration/copy failures enter removal when migration fails or is impossible.
Process each bridge independently. File ownership is determined by generation contract section 18, not an additional
content, hash or concurrent-snapshot verdict. Actual filesystem failures must be reported.

<a id="self-check-post-load-self-check-requirements"></a>

## Post-load self-check requirements

[Section 3](self-check-contract.md#self-check-3-additive-migration-and-owner-scoped-recovery) below defines migration-first self-check and its backup/removal fallback. Wait for
native loading, mod initialization, PDX caching and active asset batches; observe both current
readiness and completion events so late mod loading is covered. Include cached user PrefabAssets
that failed registration without force-loading or republishing them. Validation of live references
is read-only and is separate from dependency persistence under generation contract section 17.

For required null references, inspect the exact serialized CID. If its dependency is already cached,
copy permitted source bytes without modifying live references; require restart. Complete missing
native payloads and layout through migration before considering removal. Failed or impossible
migration enters [section 3](self-check-contract.md#self-check-3-additive-migration-and-owner-scoped-recovery)'s per-UUID backup/move, then deletion only on backup-directory permission denial.
Do not unregister live prefabs. Report actual removal failures and preserve healthy current assets.

Source reads may use other bridges' valid ImportedData copies. Output ownership remains
path-string-only; supplement the current owner's independent snapshots before acceptance.

Self-check must supplement missing copies of other mods' materials and their required non-built-in
dependencies for every inspected owner, including bridges with valid live references and current
metadata. Run the creation dependency traversal before live-reference validation; do not wait for
missing materials or a version change. Keep existing complete copies unchanged, preserve UUID/CID
and geometry, and report new copies as migration requiring restart. Resolve original, packaged, current-owner or other-owner source files. Confirmed missing sources and byte conflicts
remain migration failures under the policy above. Complete shared cache-relation preparation before
owner migration or structural inspection; record shared preparation errors without globally retiring owners or aborting the run. Attribute an actual required owner read failure to its repair/recovery flow. A separately unresolved shadowed file identity prevents accepting that owner as current. Valid
other-owner sources may supply the required local copy.

<a id="self-check-unavailable-optional-content"></a>

## Unavailable optional content

During self-check, every confirmed owner must either already have the current format and complete
required resources, migrate successfully, or enter owner-scoped recovery. Do not defer or silently
skip an owner because DLC/mod/VT content is unavailable, its cached Prefab is unresolved, or an
ordinary owner read failed. Try existing owner snapshots and permitted alternate sources first.
If these cannot supply the required data, back up/remove that owner under [section 3](self-check-contract.md#self-check-3-additive-migration-and-owner-scoped-recovery). This supersedes
the former optional-content deferral. Material availability rules still apply to actual material
loading and generation; they do not exempt saved owners from self-check.

<a id="self-check-obsolete-custom-program-retirement"></a>

## Obsolete custom-program bridge retirement

Legacy bridges using Bridge Builder owner-persisted custom shader programs are explicitly outside backward compatibility, including previously published versions. Self-check alone identifies this obsolete protocol from concrete owned program payloads or serialized legacy program/template references and removes the affected owner through its existing backup/removal and restart flow. Do not migrate, restore or reinterpret those programs. Age, style, a missing mod program or an unrelated shader error is not sufficient evidence. Healthy noncustom bridges retain the ordinary additive migration and preservation policy. This exception grants no deletion authority to generation, preview, loading, export or UI operations.

Classify only after ordinary readiness and owner discovery, before attempting legacy shader restoration or dependency supplementation for that owner. Ownership still uses the established UUID path rule; content evidence selects this retirement reason, not ownership. Inspect the confirmed owner's serialized graph and associated old program protocol at the file IO boundary. A filename extension, folder name alone, source shader name or arbitrary template hash must not classify unrelated assets. Current stable mod-program identities are not legacy signatures. Record the concrete marker/reference that selected the owner.

A positive classification directly enters the existing owner-scoped backup/move path; no shader migration is attempted. Preserve sources, other owners and shared unowned dependencies. Backup-directory permission denial alone permits the existing deletion fallback; other IO/permission failures retain their established handling. No live Prefab unregister or normal-runtime deletion is added. Report backed-up removal, removal without complete backup or unfinished truthfully, then apply the existing persistent-change restart rule. A second cold inspection finds removed owners absent and does not repeat the notice.

<a id="self-check-3-additive-migration-and-owner-scoped-recovery"></a>

## Additive migration and owner-scoped recovery

Self-check exclusively owns Bridge Builder old-version compatibility. Maintain only adapters
needed to migrate formats from previously published releases into the current format. Discovery
and native deserialization needed to reach that migration boundary may retain passive historical
schema types; they must not provide old-format behavior to normal runtime consumers. Reusable
current-format writers remain shared, but historical interpretation and adapter dispatch belong
to this inspection flow. Do not invent support for unpublished intermediate formats.

Legacy migration during post-load self-check is **additive**: preserve bridge and carried-deck UUID,
CID, native prefab version, name and geometry. Save into the existing asset, never a new bridge identity.
Add private dependencies and native payloads; retain original component files. Back up existing files,
verify identities and CID sidecars after saving. On conversion failure, roll back the incomplete phase,
log the actual error and move the affected bridge to backup under the recovery policy below.
Only backup-directory permission denial permits deleting remaining confirmed owner files.
An ordinary non-permission owner read failure prevents confirming current status and enters repair, then recovery if repair is impossible; required read permission denial propagates under section 4. Restart after successful migration; do not replace live
network registrations. Healthy already converted assets need no rewrite. The explicitly
authorized shared legacy-name cleanup still requires reference checks before retiring old files.

<a id="self-check-self-check-order-migrate-back-upmove-delete"></a>

## Self-check order: migrate, back up/move, delete

**Except for the explicit required-filesystem-permission exception in section 4, self-check must finish normally: retire explicitly classified obsolete custom-program owners and attempt repair of every other non-current or unresolved confirmed owner, and move repair-failed bridges to backup. Only permission denial at the backup directory permits deletion of that failed bridge's confirmed owned files.**

Apply this sequence per bridge. First apply [obsolete custom-program retirement](#self-check-obsolete-custom-program-retirement); all other owners attempt version migration using the shared generation
format: complete missing fields and dependencies, preserve bridge UUID/CID and valid values,
and normalize file layout/naming. Already valid current assets remain unchanged. Successful
migration is reported as migration and must not fall through to removal. If migration fails or
required information/dependencies make migration impossible, record the reason, roll back the
incomplete migration phase, and proceed to recursive backup/move. Do not skip possible migration
merely because an asset is old or has missing fields. A backup copy alone does not complete removal;
the failed asset must also leave the active asset directories.

For a bridge positively classified for obsolete custom-program retirement or whose repair actually failed, move only its confirmed UUID-owned files and
owner-local dependency snapshots to the backup directory. An ordinary owner read failure must first attempt permitted repair sources; if repair is impossible, recover that confirmed owner. Unknown discovery scope is not an owner verdict. Required read/write/move/delete permission denial instead follows section 4.
Never move/delete a source mod, another bridge's copies, shared unowned dependencies or paths
whose ownership cannot be established. A copied backup with the source still active is not a
completed move. Keep a truthful account of each successful move and every remaining source.

If the backup directory denies permission to create/write the destination, delete the remaining
confirmed files of that repair-failed owner under the user's explicit fallback policy. Do not
generalize this authorization to disk-full errors, locks, missing volumes, path errors, source
read failures or arbitrary backup exceptions. For non-permission backup failure, retain unmoved
files and record them as unprocessed; preserve successful partial backups. If deletion also lacks required owner permissions, retain the remaining paths and raise the
explicit exception in section 4. Other deletion failures remain unprocessed while independent work continues. Never pretend to override OS permissions or report successful deletion without it.

Use the existing `b{uuid}` string-only ownership rule and intended asset roots. This fallback applies
only to bridges selected for removal, not healthy bridges or unrelated assets. Process all selected
paths and bridges even when one operation fails. Verify that selected sources are absent before
reporting removal success. Record backup failure separately from deletion failure: backup-directory
permission denial followed by verified deletion is removal without a complete backup. If permissions or
another filesystem error also prevent deletion, report removal failure and the remaining paths;
never claim success or imply that the mod can override operating-system permissions.

Self-check still does not unregister live prefabs or promise to repair the current game session.
Require restart after removal; if removal fails, restarting alone does not resolve retained assets.

<a id="self-check-migration-failure-falls-back-to-removal"></a>

## Migration failure falls back to removal

After failed dependency, native-conversion or legacy naming migration leaves an unusable bridge,
restore incomplete changes from that phase, then retire only the affected owner through recursive
backup/move. Process owners independently; do not unregister live Prefabs, and require restart.
Only backup-directory permission denial permits deleting remaining confirmed owner paths; other backup failures retain unmoved files. Report removal
as removal, not migration success; verify absence and report actual failures. A layout collision must never overwrite another owner; if it prevents completing the selected owner migration, recover only the selected owner. Never claim files were removed while they remain.

<a id="self-check-operational-meaning-of-noexcept"></a>

## Operational meaning of noexcept

Self-check must normally complete; merely catching a top-level exception and returning with a
half-repaired bridge does not satisfy the requirement. Expected owner read failures enter repair or owner-scoped recovery;
repair failures follow the existing rollback and owner-scoped backup/removal flow. Continue
independent work, preserve completed results and normally finalize. Do not invent success, silently
leave partial changes unexplained, or emit the old global `Interrupted` result for controlled failures.

The user authorizes one explicit exception: when any required read, write, move or delete operation lacks filesystem permission, throw a
specific required-filesystem-permission exception. Required owner/source reads must not be treated
as ordinary read skips; required writes and source moves must not be hidden as repair outcomes. Preserve the original permission error, owner and remaining paths.
All outer entry/callback/report boundaries must let this exception propagate. Do not swallow it,
continue silently, or automatically restart to retry unchanged permissions. Backup-directory denial
with successful owner deletion is normal fallback. Ordinary non-permission owner read failures follow repair/recovery. Locks, full disks,
missing volumes and other IO failures are not automatically permission failures.

Contain other ordinary managed exceptions at automatic/manual entries, readiness/load callbacks,
queued completion, reporting, logging and cleanup. These catches are the last safeguard, not the
main expected-failure flow. Keep transactions and rollback, real partial outcomes and a guarded,
nonrecursive diagnostic path. Global unowned resource errors cannot authorize deleting all bridges.
This does not promise prevention of native crashes, OOM, process termination or uncatchable failures.
The trusted-internal-execution rule remains unchanged.

<a id="self-check-display-triggered-automatic-restart"></a>

## Display-triggered automatic restart

Immediately after successfully displaying a result notice for actual persistent bridge-file changes,
invoke the existing automatic game restart mechanism. Do not wait for a close button, confirmation,
Escape, countdown selection or optional restart action. If formatting/display fails after changes,
log the actual failure and request the same restart. Prevent duplicate restart requests per process.

The latest user instruction authorizes the product's existing normal-exit and relaunch helper,
overriding the prior product-only helper prohibition and manual-relaunch alternative. Agents still
must not start or close the player's game. This task does not introduce a global Harmony Load gate,
world-transition lock or generation-isolation subsystem. Keep inspection at its existing ready-main-
menu boundary; automatic restart does not authorize discarding unsaved city/editor progress.

<a id="self-check-ordinary-runtime-behavior-after-inspection"></a>

## Ordinary runtime behavior after inspection

The automatic restart request handles the post-inspection transition. Do not latch a global
`RestartRequired` flag that rejects preview, creation, regeneration, activation, unlock evaluation,
loading, deletion or pack management merely because inspection ran or changed files. Do not emit
`BridgeRestartRequired` as a synthetic runtime error or silently fail those operations for that reason.
Retain actual filesystem-permission exceptions, IO/native failures, native unlock rules, in-flight
inspection scheduling and normal lifecycle/disposal behavior. Restart helper failures remain truthful
logs; external process launch is not guaranteed.

Preserve the narrower write protection that prevents a stale loaded owner from overwriting that
same owner's newly migrated disk files. Select changed/retired owners individually; this is not a
global operation blocker or a reason to reject unrelated healthy bridges. Session stale-reference
bookkeeping for inspection retries also remains distinct from ordinary runtime errors.

<a id="repository-post-load-self-check"></a>

## Post-load self-check

Related requirements: [Material Contract](material-contract.md#repository-post-load-self-check-part-2).

Follow the [self-check contract](reading-map.md#bridge-self-check-contract) for all
inspection, migration/recovery, normal-completion, result UI and mandatory restart requirements.

