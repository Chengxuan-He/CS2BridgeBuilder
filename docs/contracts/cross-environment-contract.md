# Cross Environment Contract

Required companion: [Cross Environment Spec](../specs/cross-environment-spec.md). Both documents are normative; read applicable requirements from both. See the [complete scope maps](reading-map.md) for additional relocated sections.

This document is normative within its stated scope. Responsibility-based placement does not
change requirement strength, existing specialist exceptions or explicit user authorizations.
Specifications are equally normative; this separation introduces no new precedence hierarchy.

<a id="cross-environment-1-scope-and-activation"></a>

## Scope and activation

This contract governs manually requested compatibility testing and the pre-release test round.
It supplements the [project contract](reading-map.md#project-contract), [generation contract](reading-map.md#bridge-generation-contract),
[material contract](reading-map.md#material-generation-contract) and their existing ownership and acceptance rules.
It does not add a test suite to normal mod startup, preview or generation.
The [always-applicable portability requirements](../specs/cross-environment-spec.md#always-applicable-portability-requirements)
govern every task independently of these test triggers. Implementers and reviewers must apply
them to all affected producers and consumers; a test round being inactive does not suspend them.

Run the suite when the user explicitly requests it. A release request authorizes one applicable
cross-environment round before publication, within step 1 of the root release workflow. Do not
rerun it on every game launch. Reuse results for the identical candidate and recorded environment;
if code, packaged resources or a relevant environment changes, rerun the affected cases before
calling that candidate tested. Ordinary startup registration and asset self-check retain their
existing behavior and are not this compatibility suite.

Agents must not launch or close the game or launcher. The user starts the game, performs the
required in-game actions and restarts it when requested; agents may prepare isolated offline
fixtures and inspect authorized logs and outputs. Authorization to test or release does not
waive this manual-start rule.

<a id="cross-environment-2-one-agent-and-isolated-workspace-per-environment"></a>

## One agent and isolated workspace per environment

Assign one independent sub-agent to each concrete environment in an actual test round. The main
agent coordinates the matrix and aggregates evidence. An agent handles only one environment at
a time; reuse existing agents and batch assignments when slots or machines are unavailable.
A documentation-only change to this contract does not require launching environment agents.

Before execution, provide each environment agent its own writable project, harness, cache,
outputs and logs. Share immutable inputs only. Do not share writable Unity state or GPU authoring
projects between agents. A local folder labelled AMD or GeForce NOW is not that environment.
Without access to the required host, leave that environment Blocked or NotRun and state why.
Preserve evidence outside disposable environments before coordinator-managed cleanup.

<a id="cross-environment-4-safe-permission-and-mapping-fixtures"></a>

## Safe permission and mapping fixtures

Use newly created, bounded test directories and disposable copies. Never modify ACLs on real user
assets, the game installation, system directories or unrelated profile data. Preserve the original
ACL and file attributes, retain the owner's permission-restoration rights, and restore them in
`finally`, including on cancellation and failure. Verify restoration and unchanged retained bytes.
If safe restoration cannot be guaranteed, do not apply the restriction; record the case NotRun.
Do not change global environment variables, machine security policy or installed driver/compiler
files to simulate another system.

Distinguish the installation directory from the game's writable profile. Native user data,
`ImportedData`, mod settings/logs and external recovery/export locations have different roles.
Denying access outside the installation may also deny required native game storage. Exercise each
boundary separately. If essential profile/output storage cannot be written, report the actual IO
failure and preserve existing assets; do not bypass permissions, fabricate success or silently
redirect persistence into another owner's directory. An inaccessible path is not evidence that
its assets are absent or damaged. Existing explicitly selected deletion/recovery rules remain
separate; a permission fixture does not authorize retirement of healthy assets.

Record mappings before reading or writing, preserve logical owner identity, and verify resolved
paths remain inside the intended fixture. Include spaces, Unicode and long paths where supported;
report local, cross-volume and network mappings separately. Do not create recursive junctions,
follow an unbounded alias graph or delete unrelated junction-target data during fixture cleanup.
Exercise authorized removal of disposable owned payloads through mappings as well as ordinary paths;
fixture cleanup restrictions must not become production bans on mapped-directory operations. Missing source content,
unavailable network shares and access denial must remain distinguishable operational outcomes.

<a id="cross-environment-6-results-and-release-decision"></a>

## Results and release decision

Each matrix row has one explicit status: Passed, Failed, Blocked or NotRun. Passed requires the
specified cases to finish in that actual environment. Record the precise reason and missing step
for every other status. API fakes, a different GPU or an unavailable cloud session cannot turn a
Blocked/NotRun row into Passed. State tested and untested combinations, rather than claiming
universal compatibility or that all environments passed.

Before publication, complete one round of the applicable declared matrix and present its results.
If any required case failed or remains Blocked/NotRun, obtain the user's explicit decision whether
to release with those disclosed gaps; the original release request alone is not that decision.
Continue independent preparation while awaiting it, but do not publish first and seek acceptance
afterward. Subsequent official package download and installation verification remain mandatory
under the existing four-step release workflow.

This rule governs subsequent release decisions. It does not retroactively revoke or relabel a
release already completed before the rule was requested. Historical tests remain evidence only
for their recorded build, environment and scope. Store normative rules here, per-round results
under `docs/archive/audits/`, and raw logs/captures outside publication inputs. Documentation-only
maintenance neither starts a test round nor requires a build, deployment or publication.

