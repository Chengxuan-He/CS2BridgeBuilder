# Development material checkpoint

The dev branch contains the native material resources required by the current
movable-bridge implementation, including the five Draw01 units previously supplied
as a private development overlay (50 files, 21,205,423 bytes). They are included so
a clean checkout can build and resolve the same diagnostic inputs; their presence
is not filtering, appearance, package-size or complete-owner acceptance.

Candidate18 passed compilation, numerical resource checks and twelve-language
checks, plus the scoped offline consumer tests recorded during development. Candidate18 DLL SHA256 is
`C9FFC315C9CD5CB5DE701CDB19EBB794B24131335EA69C61EA1A1B084E4D7A60`.
It was deployed locally for diagnosis with original files and generated assets
backed up; real-game acceptance has not occurred. Known unresolved gates include repeat seam filtering,
non-affine source frames, unsupported native layer representation, the 55 MB
package limit and complete 10 MB owner closure. Integer-grid and two-period trials
do not remove these gates. The experimental double-period output is not included.

Local artifacts, game assemblies, decoded source dumps, caches, backups and native
test logs are excluded from Git. Offline analysis tools may require separately
available game source inputs; runtime uses the checked-in measured tables and
prepared resources and does not require those authoring dumps. No release version
or production acceptance is declared by this development checkpoint.
