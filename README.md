# Upgrade Filtering

A BepInEx mod for MycoPunk that adds advanced filtering and customizable priority sorting to the gear upgrade menu.

Originally split out from Enhanced Upgrade Menu as a focused standalone mod.

## Features

### Filter Panel
- Toggle a filter panel from the gear details window
- Hide upgrades by rarity (Standard, Rare, Epic, Exotic, Oddity)
- Filter by favorites (show all, only favorites, hide favorites)
- Filter by upgrade stats / properties (context-aware for upgrades vs skins)
- Clear all filters with one click

### Priority Sort
- Fully customizable multi-criteria sort order
- Drag-and-drop reordering of sort priorities
- Criteria include favorites, locked/unlocked, rarity, trash/turbo status, recently used/acquired, and name
- Priority order is saved between sessions

### Stat Display Formatting
- Optional reformatting of upgrade stats from `50 Damage` to `Damage: **50**`
- Does not affect directive window hover information
- Toggleable in config

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8
* [HarmonyLib](https://github.com/pardeike/Harmony) (included via NuGet)

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode to generate the .dll file

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Via Thunderstore (Recommended)**:
1. Download and install via Thunderstore Mod Manager
2. The mod will be automatically installed to the correct directory

**Manual Installation**:
1. Place the built `UpgradeFiltering.dll` in your `<MycoPunk Directory>/BepInEx/plugins/` folder

### Executing program

The mod loads automatically through BepInEx when the game starts. Check the BepInEx console for loading confirmation messages.

## Configuration

Access mod settings through the BepInEx configuration file at `<MycoPunk Directory>/BepInEx/config/sparroh.upgradefiltering.cfg`.

- **Reformat Statistics**: Enable/disable Key: Value stat reformatting (default: enabled)

Priority sort order is stored via the game's player options (`SortPriority.Order`).

## Usage

1. Open a gear details window
2. Click **Filter** to open the filter panel
3. Click **Priority Sort** to open the drag-and-drop priority editor
4. Save your priority order to apply a custom multi-criteria sort

## Help

* **Mod not loading?** Verify BepInEx is installed correctly and check console logs for errors
* **Filter panel missing?** Open gear details first, then click the Filter button
* **Priority sort not applying?** Open Priority Sort, arrange criteria, then click Save
* **UI elements missing?** Confirm mod version compatibility and verify no other mods are interfering

## Authors

- Sparroh

## License

This project is licensed under the MIT License - see the LICENSE file for details
