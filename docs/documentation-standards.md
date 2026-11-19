# Documentation standards

## Scope and authority

These rules govern all project documentation, including this document. Read them before creating,
editing or relocating documentation. Document classification assigns responsibilities; it does not
change requirement strength, introduce a precedence hierarchy or cancel specialist exceptions and
explicit user authorizations. Specifications and contracts remain normative within their scopes.

## Document responsibilities

| Type | Responsibility | Excluded content |
| --- | --- | --- |
| Constitution | Stable project principles, governance and boundaries | Concrete examples, implementation procedures, measurements and audit findings |
| Specification | Required observable behavior, algorithms, formats, constraints and acceptance criteria | Audit records, measured bridge data and implementation history |
| Contract | Responsible parties, interfaces, ownership, preconditions, postconditions and failure obligations | Implementation steps, concrete audit results and measured bridge data |
| Architecture | Actual modules, their relationships, dependency directions and implemented design | Unverified implementation claims, normative requirements disguised as observations and raw audit records |
| Guide | Executable contributor or operator procedures and prerequisites | Independent replacement requirements and historical result logs |
| Audit | Concrete geometry and code inspections, all concrete bridge measurements, source evidence and verification limits | New normative policy and unsupported generalization beyond the inspected scope |
| Incident investigation | A specific failure's reproduction conditions, observations, hypotheses, diagnosis, repair attempts and verified outcome or unresolved status | General requirements maintained independently of specifications/contracts and unverified claims of resolution |
| Implementation and acceptance record | Build- or revision-scoped implementation stages, decisions, completed checks, pending acceptance and handoff status | A second maintained implementation specification, executable current guidance disguised as history and claims that old results verify the current build |
| Bridge/source evidence record | Asset- or style-specific source provenance, measured parameters, geometry/LOD observations and reproducible evidence with applicability limits | Universal bridge rules, runtime recipes invented from unsupported inference and duplicate copies of the same audit |
| Publishing content or preparation brief | Maintained public-facing descriptions, information fields, image briefs and release-preparation material, with draft/verified status explicit | Proof that publication occurred, replacement technical requirements and unsupported product claims |
| Publishing record | Release descriptions, publication state and evidence for a specific release | Replacement development requirements |
| Index or reading map | Navigation to canonical topics and exact complete-reading scopes | Duplicated technical bodies, audit results and repeated normative content |
| Agent instruction entry point | Repository- or directory-scoped agent obligations and routing to their authoritative reading scopes | Hidden overrides, duplicated detailed requirements and unrelated implementation/audit bodies |
| Documentation standards | Rules for authoring, organizing and maintaining documentation | Concrete examples, project implementation descriptions and audit records |

Separate mixed passages by responsibility. Keep required behavior in the specification and the
responsible interface obligations in the contract. Place actual implementation relationships in
architecture, behavioral mechanisms and technical rationale in specifications, interface and ownership
mechanisms in contracts, and operational steps in guides. Place all concrete
bridge measurements and geometry or code audit findings in audit documents, even when they support
a current requirement. Link evidence from the responsible requirement without reproducing it there.
Documentation standards must obey their own classification and contain documentation rules only.

## Locations and navigation

Maintain project documentation under `docs/`. Use `constitution.md`, `documentation-standards.md`
and `architecture.md` for their respective project-wide roles. Use `specs/`, `contracts/`,
`guides/` and `publishing/` for their corresponding document types. Maintain evidence records under
`archive/` using the responsibility and subject subdivisions below. Their location does not make
recorded evidence current or obsolete by itself; state its applicability explicitly.

## Project documentation directory tree

This tree defines document placement and navigation, not a second inventory of every topic file.
Angle-bracket entries represent naming patterns or subject groupings; the other directory names
are the maintained library locations. Keep topic inventories in their README indexes.

```text
<repository>/
|-- AGENTS.md                         Repository agent instructions and required reading
|-- README.md                         Project entry point and links to canonical documentation
|-- docs/
|   |-- README.md                     Documentation index
|   |-- documentation-standards.md    Document types, placement and maintenance rules
|   |-- constitution.md               Stable project principles and governance
|   |-- architecture.md               Actual modules, relationships and dependencies
|   |-- contracts/
|   |   |-- README.md                 Contract topic index
|   |   |-- reading-map.md            Complete mandatory-reading scopes
|   |   `-- <topic>-contract.md        Interfaces, ownership and lifecycle obligations
|   |-- specs/
|   |   |-- README.md                 Specification topic index
|   |   |-- metadata-spec.md           Publishing metadata and template rules
|   |   |-- lod-spec.md                Canonical LOD behavior and acceptance
|   |   `-- <topic>-spec.md            Behavior, algorithms, formats and acceptance criteria
|   |-- guides/
|   |   |-- README.md                 Contributor/operator guide index
|   |   `-- <procedure>.md            Executable procedures and prerequisites
|   |-- publishing/
|   |   |-- README.md                 Publishing navigation
|   |   |-- <publication-content>.md  Descriptions, preparation briefs and information fields
|   |   |-- release-<version>.md      Version-specific publication records
|   |   `-- <supporting-artifact>     Changelogs, images and SDK draft inputs
|   `-- archive/
|       |-- README.md                 Evidence and historical-record index
|       |-- audits/                   Inspections, measurements and verification evidence
|       |   `-- <subject>/            Optional focused audit grouping, such as width
|       |-- incidents/                Failure investigations and repair evidence
|       |-- implementation/           Revision-scoped implementation and acceptance records
|       |-- bridge-evidence/          Cross-style or source-focused bridge evidence
|       `-- bridge-styles/
|           |-- README.md             Style-specific evidence index
|           `-- <StyleId>/            One style's audits, incidents and implementation records
`-- <source-or-tool-directory>/
    |-- AGENTS.md                     Scoped instructions where needed
    `-- README.md                     Local entry point linking to canonical library guidance
```

Incident investigations, implementation records and bridge/source evidence are specialized evidence
types, not additional normative authorities. `bridge-styles/<StyleId>/` is a subject grouping rather
than a separate document type: classify each record by its actual purpose and link to it from other
indexes instead of copying it into multiple archive subdivisions. Retain existing artifact-relative
paths when needed to interpret historical tool records.

Publishing content and completed publication records have different status even when they share
`publishing/`. Supporting images, text changelogs, logs and SDK inputs are artifacts, not new
normative document types; retain the formats required by their consumers. Raw evidence needs a
record identifying its source and verification scope. Temporary work and retired-file backups are
outside the canonical documentation tree. There is no separate Design document type or directory:
merge its behavioral content into specifications, interface obligations into contracts, actual module
relationships into architecture and recorded observations into the appropriate evidence record.

## Navigation and canonical ownership

Root and directory `AGENTS.md` files remain instruction entry points. README files route readers
to canonical documentation. The root README additionally retains the complete contributor workflow for GitHub readers, as explicitly requested by the user; keep that embedded guide synchronized with its canonical guide, including rebased links. Do not introduce other duplicate technical bodies. Chat output
directories are not maintained project-documentation locations; do not put canonical technical documents,
duplicate bodies or replacement navigation documents there. Raw artifacts and previously delivered
evidence are not automatically maintained documentation.

Keep one canonical body for each responsibility and use repository-relative links. Update indexes,
reading scopes and affected directory instructions together when content moves. Preserve existing
anchor destinations or update every known referring link. Keep mandatory-reading coverage intact.

## Language and presentation

Follow the constitution's [documentation language and publishing-description rules](constitution.md#project-all-documentation-is-written-in-english), including documents delivered outside the repository. Keep internal descriptions and examples in English; publishing language, official titles and template preservation follow the [metadata specification](specs/metadata-spec.md). Translate other non-English descriptions and examples into English and consolidate equivalent translations without losing unique facts or requirements. Record any necessary exact-source exception and its English explanation; preserve runtime translations, third-party licenses and evidence under that rule.

Use Markdown unless explicitly required otherwise. Use one H1 title and logically ordered H2
sections. Keep headings descriptive and order content by responsibility and dependency; order
workflow descriptions by execution. Use CommonMark lists and tables with consistent whitespace.
Use code blocks for literal commands, formulas and diagrams. Mermaid flowcharts belong only in
genuine workflow sections of specifications under `docs/specs/`. Every such section must include a
flowchart reflecting its actual sequence, branches and exceptions. Flowcharts are prohibited in all
non-specification documents and in non-workflow specification sections. Parameters, constraints,
reference tables and unordered obligations must not be relabelled or rearranged as workflows to
justify a diagram. Split oversized workflow diagrams at coherent phase or module boundaries,
with explicit handoffs and ordering consistent with execution.
Use consistent descriptive filenames and avoid names that misrepresent the document type.

## Evidence and normative text

Every documented native LOD formula must cite its verified native source: the inspected local
Game.dll version and SHA-256, fully qualified class and method, and an audit location with
the relevant IL offsets or decompiled lines and their extraction identity. Place the citation
beside the formula or route it to that exact evidence record. A method name, plausible result
or passing reconstruction test alone is insufficient. Do not label algebraic inversions,
fitted relationships or project-authored compensation as native formulas. An explicitly
authorized mod-defined LOD formula must instead be labelled as a project derivation, cite
its native premises and applicable authorization scope, and state its tradeoffs and evidence
limits. Remove an LOD formula without the applicable provenance from maintained documentation,
including audits, retaining supported observations and original-file backups separately.
This requirement concerns LOD formulas; it makes no claim that formulas in other domains are native.

Keep audit evidence traceable to its sources, inspected inputs, implementation identity, conditions
and verification limits. Distinguish observed facts, derived conclusions, unverified hypotheses and
pending acceptance. Preserve concrete numeric data and diagnostic quotations exactly. Do not
promote a measured case into a universal requirement without explicit authorization.

Integrate authorized requirement changes into the current normative paragraphs. Specifications must
not contain revision dates, changelogs, superseded alternatives, branch baselines or rollback records.
Evidence timestamps are not specification revision dates. Preserve existing operational backup and
asset-recovery obligations independently of documentation maintenance.

## Preservation and maintenance

Relocation must preserve every effective requirement and all unique evidence. Do not remove data
merely to make a document shorter. Split normative statements from their supporting concrete cases
and retain both in their appropriate destinations. Do not retain contradictory obsolete instructions
as competing rules; reconcile them with current user-authorized requirements without fabricating
policy. Label historical evidence so that it cannot be mistaken for a current acceptance result.

Keep the automatically discovered instruction chain below the combined 32 KiB limit. Put detailed
rules in the appropriate canonical documents and route readers to the exact required scope.
Directory instructions remain additive unless they explicitly tighten their scope. Do not create
permanent `AGENTS.override.md` files that hide normal repository instructions.

Validate document links, anchors, reading coverage, classification and preservation after a move.
Documentation-only changes do not require a runtime build, game shutdown or deployment.
