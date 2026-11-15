# Runtime Bridge Generation Instructions

## Scope and required reading

Applies to `src/BridgeBuilder/**` in addition to [root instructions](../../AGENTS.md). Before editing, read the complete [project contract](../../docs/contracts/reading-map.md#project-contract), [generation contract](../../docs/contracts/reading-map.md#bridge-generation-contract) and [runtime invariants](../../docs/contracts/reading-map.md#movable-runtime-invariants). Numbered rules refer to the generation contract. Read the full [material contract](../../docs/contracts/reading-map.md#material-generation-contract) for material work and [movable sections 16–24](../../docs/contracts/reading-map.md#movable-sections-16-24) before movable changes.

Read all scoped destinations below before editing.

## Scoped requirements

- [Generation and execution](../../docs/contracts/generation-contract.md#runtime-generation-and-execution)
- [Materials, ownership and persistence](../../docs/contracts/persistence-contract.md#runtime-materials-ownership-and-persistence)
- [Verification and delivery](../../docs/contracts/development-contract.md#runtime-verification-and-delivery)
