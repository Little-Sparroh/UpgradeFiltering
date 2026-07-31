# Upgrade Filtering

A BepInEx mod for Mycopunk that adds advanced filtering and customizable priority sorting to the gear upgrade menu.

Originally split out from Enhanced Upgrade Menu as a focused standalone mod.

## Features

### Filter Panel

- Toggle a filter panel from the gear action bar (**Filter**)
- Hide upgrades by rarity (Standard, Rare, Epic, Exotic, Oddity)
- Filter by favorites (Show All, Only Favorite, Hide Favorite)
- Filter by upgrade stats / properties (context-aware for upgrades vs skins)
- Clear all filters with one click

### Priority Sort

- Fully customizable multi-criteria sort order
- Open from the gear action bar (**Upgr. Sort**)
- Drag-and-drop reordering of sort priorities
- Criteria include favorites, locked/unlocked, rarity, trash/turbo status, recently used/acquired, and name
- Save, Cancel, and Reset controls
- Priority order is saved between sessions

### Stat Display Formatting

- Optional reformatting of upgrade stats from `50 Damage` to `Damage: **50**`
- Does not affect directive window hover information
- Toggleable in config (disabled by default)

## Getting Started

### Dependencies

- Mycopunk (base game)
- [BepInEx](https://github.com/BepInEx/BepInEx) 5.4.2403 or compatible (BepInExPack for Mycopunk)
- [SparrohUILib](https://thunderstore.io/c/mycopunk/p/Sparroh/SparrohUILib/) 1.2.2 or compatible

### Building

1. Clone this repository
2. Open the solution in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode to generate the `.dll`

Alternatively, use the .NET CLI:

```bash
dotnet build --configuration Release
```

### Installing

**Via Thunderstore (recommended)**

1. Install via a Thunderstore mod manager
2. Dependencies (including SparrohUILib) are installed automatically

**Manual installation**

1. Install BepInEx and SparrohUILib
2. Place `UpgradeFiltering.dll` in `<Mycopunk Directory>/BepInEx/plugins/`

The mod loads automatically through BepInEx when the game starts. Check the BepInEx console for a load confirmation
message.

## Configuration

Settings are in:

`<Mycopunk Directory>/BepInEx/config/sparroh.upgradefiltering.cfg`

| Setting         | Section | Default | Description                                     |
|-----------------|---------|---------|-------------------------------------------------|
| Enable Reformat | General | `false` | Force `Key: Value` stat format on upgrade stats |

Priority sort order is stored via the game's player options (`SortPriority.Order`).

## Usage

1. Open a gear details window
2. Click **Filter** on the gear action bar to open the filter panel
3. Click **Upgr. Sort** to open the drag-and-drop priority editor
4. Arrange criteria, then click **Save** to apply (or **Reset** to restore defaults)

## Help

- **Mod not loading?** Verify BepInEx and SparrohUILib are installed, then check console logs for errors
- **Filter panel missing?** Open gear details first, then click **Filter** on the action bar
- **Priority sort not applying?** Open **Upgr. Sort**, arrange criteria, then click **Save**
- **UI elements missing?** Confirm mod and SparrohUILib version compatibility

## Authors

- Sparroh

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
