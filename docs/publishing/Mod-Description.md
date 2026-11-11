# Bridge Builder

## English

### Your roads. Your bridges.

Combine available roads or tracks with supported bridge styles in Cities: Skylines II. Choose a network and bridge style, inspect the live 3D preview, then save your bridge or place it with the game's network construction tool.

### Features

- Fixed suspension, cable-stayed, truss-arch, tied-arch and covered wooden styles, depending on installed content and supported prototypes.
- Custom movable bridges: drawbridges, lift bridges and pedestrian movable styles from Bridges & Ports. Required DLC/content must be available.
- Independent upper and lower roads or tracks on supported double-deck styles, including opposite deck directions.
- Search and filter networks and bridge styles. Use Create to save a bridge, or Create and build to enter construction mode when it is unlocked.
- Preview, rename, export, build and delete saved bridges from the management panel.
- Continuous asynchronous preview generation and viewport-sized rendering with antialiasing. Actual waiting time depends on the selected content and hardware.
- Native-material-based shaders with current-skeleton binding for supported movable surfaces and decals, with owner-local persisted dependencies.
- A multilingual interface and configurable shortcut (Ctrl+B by default). Bridges normally inherit their source unlock requirements; the settings can bypass development-point restrictions.

### Compatibility and requirements

[Road Builder](https://mods.paradoxplaza.com/mods/87190/Windows), [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows) and [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows) are optional integrations, not mandatory dependencies. Install the content needed for the roads or supported styles you select. The double-deck Golden Gate style comes from Bridge Expansion Pack, not the base single-deck Golden Gate prototype.

Keep Bridge Builder installed and enabled. Generated shader programs require its startup registration; operation after disabling or uninstalling the mod is not supported. Required game/DLC resources remain prerequisites. Saved bridges have their own dependency snapshots; updating the mod does not regenerate their geometry.

There is no reliable fully automatic importer for arbitrary custom bridge assets. Supporting a new style requires bridge-specific measurements, implementation and visual checks. A coding agent does not guarantee a correct result in one attempt. See [Adding a bridge style with a coding agent](https://github.com/Chengxuan-He/CS2BridgeBuilder#adding-a-bridge-style-with-a-coding-agent). Compatibility with every third-party asset or network/style combination is not promised.

### Important notes

- Back up your city before removing a bridge. **Deleting a generated bridge also removes its placed instances.**
- If a self-check reports changes and requests a restart, restart the game before continuing.
- Recreate older bridges affected by a generation fix when needed; preserve unaffected bridges and back up your save first.
- Report the game/mod versions, bridge style, selected networks, screenshots and relevant logs when something goes wrong. Rendering and motion can require further verification for particular combinations.

Source: [GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder). Original project code is MIT-licensed; game and third-party assets retain their respective licenses.
