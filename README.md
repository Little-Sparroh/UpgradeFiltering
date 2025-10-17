# ImprovedUpgradeSorting

A BepInEx mod for MycoPunk that provides advanced filtering and sorting options for upgrade lists in gear detail windows.

## Description

This client-side mod revolutionizes upgrade management in MycoPunk by adding a comprehensive filtering and sorting system to gear detail windows. Access filters by clicking the bright purple "FILTER" button in any gear upgrade interface. The filter panel allows hiding specific rarities, filtering by favorite status, and showing only upgrades with certain stat properties.

The mod includes property-aware sorting that properly handles upgrade names, rarity-ordered sorting, and recently collected upgrade prioritization. The filtering system dynamically adapts to different gear contexts (upgrades vs skins) and supports complex stat-based filtering for advanced upgrade hunting.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Option 1: Via Thunderstore (Recommended)**
1. Download and install using the Thunderstore Mod Manager
2. Search for "ImprovedUpgradeSorting" under MycoPunk community
3. Install and enable the mod

**Option 2: Manual Installation**
1. Ensure BepInEx is installed for MycoPunk
2. Copy `ImprovedUpgradeSorting.dll` from the build folder
3. Place it in `<MycoPunk Game Directory>/BepInEx/plugins/`
4. Launch the game

### Executing program

Once installed, filtering functionality is available in all gear detail windows:

**Accessing Filters:**
1. Open any gear details window
2. Look for the bright purple "FILTER" button
3. Click to toggle the filter panel
4. Use filters to customize your upgrade display:
   - **Rarity Hiding:** Click rarity names to hide/show upgrades of that rarity
   - **Favorite Filtering:** Choose to show all, only favorites, or hide favorites
   - **Stat Filtering:** Select specific stat properties to show only upgrades with those attributes
   - **Clear All:** Reset all filters with the clear button

**Sorting Options:**
- Enhanced sorting that respects upgrade instance names
- Proper rarity ordering (Oddity > Exotic > Epic > Rare > Standard)
- Recently collected upgrades sorting
- Automatic filter reapplication after sorting changes

The system automatically adapts to upgrade vs skin context and applies filters in real-time.

## Help

* **Filter button not showing?** Make sure you're in a gear details window and it may take a second to load
* **Filters not applying?** Click away from the filter panel to apply changes
* **Wrong stat properties?** Stat filters automatically adapt to the current gear type
* **Sorting not working?** The mod enhances existing sorting - try changing sort mode in gear window
* **Performance issues?** Filters are only active when the filter panel is open
* **UI elements overlapping?** The filter panel positions itself to avoid conflicts with other UI
* **Lost my filter settings?** Filters reset when closing the gear window - use favorites instead

## Authors

* Sparroh
* funlennysub (original mod template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
