# Material routing fixtures

This directory contains isolated Unity Editor fixtures, not a player startup pipeline.
Use the [material verification requirements](../../docs/specs/material-spec.md#material-verification-requirements)
and [material contract](../../docs/contracts/material-contract.md) to select applicable current behavior.
Fixture filenames containing native revision numbers do not establish compatibility with the current DLL.
Inspect the extant source entry point and its argument parser before running it with version-matched
native game assemblies, captured inputs and a candidate mod DLL in a private Unity environment.
These fixtures must not start the game or modify player bridge owners.

The [fixture history](../../docs/archive/audits/material-routing-fixture-history.md) preserves the old
commands, source provenance, case counts and limitations. Retired probe names and old owner-local
program-envelope assertions there are not current runnable guidance. Native parsing or isolated
rendering does not establish live-city appearance, motion, complete VT streaming or cold loading.
