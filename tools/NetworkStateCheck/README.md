# Native network appearance state merge

The six generated state appenders are Draw01/02/03, PedestrianDraw01/02 and Lift03.
Lift01/02/04/05 append no mechanism state rules; they preserve selected-road state
arrays without this merge. The common helper removes only Pavement, Gravel and
Tiles from appended donor rules, never from selected-road rules. Mixed donor rules
retain conditions, node match type and all remaining state values.

After regenerating network data run `python tools/NormalizeMovableNetworkStates.py`.
`--check` is read-only; Build.ps1 also rejects old direct state-array Concat appends.
The transformation changes call plumbing, not embedded original measurements.

Run Network133Check.Run in an isolated Unity Editor with `-gameManaged` and
`-modAssembly` pointing at the game managed folder and candidate DLL. Optional
`-baselineRows` selects the Native132States.tsv fixture. `-recordBaseline` records
original donor rules from the unmodified native132 DLL; do not use a modified DLL
for that mode. The fixture contains state/condition names only, no player data.
The check compares 36 edge/node signatures for six styles x three selected
appearances, tests selected rule identity/immutability and generic mixed rules.
It does not prove game screenshots or repair existing saved bridges.
