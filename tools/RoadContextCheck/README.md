# Elevated road context checks

`dotnet run --project tools/RoadContextCheck/RoadContextCheck.csproj` compiles the production
`BridgeRoadContext.cs` with explicit native boundary stand-ins. It checks one resolution per
source, source/entity/clone reuse, two-source isolation, nested/disposed/failed scopes,
active-scope missing-source rejection, capture lifetime, level-only metadata and conservative
partial requirement evaluation. Stand-ins do not establish GameDB or native renderer behavior.

Run `NativeLevelCheck.Editor.cs` in the existing isolated shared Unity project with
`-executeMethod NativeLevelCheck.Run -gameManaged <game Managed> -modAssembly <candidate DLL>`.
It loads the actual candidate and native game types, calls native GetElevationFlags at 36
source-flag/physical-height combinations, evaluates native requirement arrays through the
production partial predicate, and verifies actual PreviewSession context snapshot/disposal.
It does not initialize a city database, run complete native ECS jobs or render a placed bridge.
Preserve separate game acceptance for those operations. No game assets are written.
