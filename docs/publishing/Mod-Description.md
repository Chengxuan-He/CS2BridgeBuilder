# Bridge Builder

## English

### Overview

Bridge Builder lets you combine available roads or tracks with bridge styles in Cities: Skylines II. Choose a deck and a bridge style, inspect the combination in an isometric 3D preview, then place the generated bridge using the game's network construction tool. Supported double-deck styles let you choose the upper and lower networks independently and reverse the direction of one deck relative to the other.

### Features

- Choose from available suspension, cable-stayed, truss-arch, tied-arch and covered wooden bridge styles. The selection depends on installed content and supported prototypes.
- Search roads and tracks, filter them by network type, and search bridge styles.
- Use **Create** to save a bridge, or **Create and build** to close the mod panel and enter construction mode when the bridge is unlocked.
- Search, filter, preview, rename, build and delete previously created bridges.
- Generated bridges belong to the Bridge Builder asset pack. The interface supports multiple languages.

### Getting started

Load a city and open Bridge Builder from its toolbar icon. Select a road or track and a bridge style. For a double-deck bridge, choose the second network and the deck-direction option. Review the preview and bridge name, then select Create or Create and build.

- **If the bridge self-check is triggered, please restart the game.**
- Uninstalling Bridge Builder does not remove bridges you have already created. These saved bridges remain available for placement and use. To remove them, use Bridge Builder’s bridge management tools.

### Compatibility

- Compatible with [Bridge Engineer](https://mods.paradoxplaza.com/mods/164700/Windows).

- Use Road Builder to create custom roads for your bridges. Once a bridge has been generated, deleting its source road in Road Builder does not prevent you from placing that saved bridge again. This does not mean required DLC, content mods or shared assets can be uninstalled.

- Compatible with [Road Builder](https://mods.paradoxplaza.com/mods/87190/Windows) and [Bridge Expansion Pack](https://mods.paradoxplaza.com/mods/92245/Windows). **Both mods are optional, not required dependencies of Bridge Builder.** Install them only to use their roads or supported bridge styles.
- Some bridge styles require their corresponding DLC or content mod. The double-deck Golden Gate style comes from **Bridge Expansion Pack**; it is not a double-deck mode of the base single-deck Golden Gate prototype.
- Compatible with [Node Controller](https://mods.paradoxplaza.com/mods/138523/Windows). **Node Controller is optional, not a required dependency.** The double-deck lower-railway seam fix also supports its geometry pipeline without overriding its node settings.
### Important notes

- **After updating to a version that fixes a bridge-generation problem, delete and recreate affected bridges created with older versions.** Updating the mod does not regenerate existing bridge assets. Back up your save first; deleting a bridge also removes its placed instances.
- Settings provide a configurable shortcut (Ctrl+B by default) and an option to remove development-point restrictions. Bridges normally inherit their prototypes' unlock requirements; enabling the option bypasses development unlock restrictions when building.
- Bascule bridges, pedestrian bascule bridges and lift bridges are not currently supported. Not every network/style combination is supported.
- **Deleting a generated bridge also removes its placed instances from the map. Back up your save first.** This is not simply removing a catalogue entry.
- When reporting a problem, include game and mod versions, the bridge style, selected networks, screenshots and relevant mod logs.

Source: [GitHub](https://github.com/Chengxuan-He/CS2BridgeBuilder). Original project code is MIT-licensed; game and third-party assets retain their respective licenses.
