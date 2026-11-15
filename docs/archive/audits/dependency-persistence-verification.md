# Dependency persistence verification

Retained evidence; its recorded observations do not certify current native or visual acceptance.

## Historical timing evidence

The game log for candidate86 on 2026-11-05 at 16:10:23 reports 18 owners checked in
2,565 ms: discovery 74, dependencies 764, validation 25, persistence 1, portable migration
707, names 813, layout 178 and other work 3 ms. It reports zero removals, migrations,
pending-restart copies, removal errors and migration errors. This is one observed run,
not a general timing guarantee or proof of every bridge's visual/cold-load acceptance.

## Verification tools

Reproducible source-linked checks live in `tools/DependencySourceCheck`,
`tools/AssetLayoutCheck`, `tools/SelfCheckLifecycleCheck`, `tools/LegacyNameCheck` and
`tools/NativeLocaleCheck`. Dependency fixtures prohibit global enumeration and exercise
explicit native/prepared edges, missing files, foreign owners, conflicts and restart
relations. Saved-owner replay accepts an external game-data directory and writes only
isolated test output. Local logs, user assets and backup trees are not GitHub source inputs.

The [persistence policy](../../contracts/reading-map.md#bridge-asset-persistence) and
[generation contract](../../contracts/reading-map.md#bridge-generation-contract) remain normative.

## Related responsibilities and evidence

[dependency persistence implementation contract](../../specs/persistence-spec.md).
