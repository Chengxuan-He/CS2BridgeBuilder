# Affine numerical lowering

Run `dotnet run --project tools/AffineGeometryMigration -- <repository>` after
regenerating measured geometry code and its embedded numerical arrays. This is an
offline Roslyn transformation; inspect its diff before building. It preserves
binary table indices, arithmetic grouping and authored recipe order.

It lowers simple scale, translation and affine coordinate expressions and schedules
independent ranges or source-only scatter tables through `AffineGeometry`. It
deliberately leaves topology loops, shared counters, engine calls, unknown scatter
writes and dependent writes unchanged. It is not a general parallel compiler.

The manually reviewed fixed-bridge dense coefficient caches, XYZ reconstruction
matrices and main-thread mesh snapshots are part of the implementation and must be
retained if their producers are regenerated. Do not replay historical resource
conversion backups over current geometry code.

Validate with `AffineGeometryCheck`, `tools/Test.ps1`, the affected measured geometry
checks and the production build. Compare native input/output recipes across LODs
and width parameters. Keep engine operations on the main thread and never infer
new prototype data merely to make a loop parallel.
