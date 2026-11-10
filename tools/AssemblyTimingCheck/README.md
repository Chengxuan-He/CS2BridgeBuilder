# Movable assembly timing checks

Run `dotnet run --project tools/AssemblyTimingCheck` to test the source-linked
production collector: exclusive accounting, two LODs, counts, encoding subset,
failure/finally completion, idempotence and logging failure isolation.
The Editor probe loads the actual compiled mod and checks runtime counter support.
These tests do not reproduce a player-selected road or measure its performance.
