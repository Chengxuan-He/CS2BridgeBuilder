# Shutdown event regression

`Run.ps1 -GameDirectory <installed-game-directory> -HarmonyDll <installed-mod-0Harmony.dll> -ReportDirectory <isolated-output>` embeds the installed game's Mono in a separate diagnostic host. It does not launch Cities2, Unity Player or a launcher, and does not invoke native game shutdown or remove assets.

The probe subscribes to the actual Unity `Application.quitting` event, installs a control Harmony postfix, then calls ownerless `UnpatchAll`. It verifies that the control patch is removed while Unity's actual managed quit-event delivery still invokes the event subscriber, and that unsubscribing prevents another delivery. No native engine API is substituted. This validates event survival, not the order of a live game's shutdown; ordering is checked separately against the installed native assemblies and still requires human acceptance.

Use the game Mono host, not .NET 8, for the probe: the installed older Harmony runtime-detour implementation is incompatible with the .NET 8 control-patch test. The host itself uses .NET 8 only to embed the game runtime.

The probe also constructs the installed engine's actual NoAllocReadOnlyCollection and confirms World.All uses that type: interface enumeration throws, while the public typed enumerator preserves all elements. This isolates the native collection contract without constructing or destroying an engine World.
