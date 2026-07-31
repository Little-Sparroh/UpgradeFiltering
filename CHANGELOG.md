# Changelog

## 1.1.2

### Fixes

- **Priority sort actually changes the list** — default order puts rarities **before** Name/Recently*; saved orders that
  had Name above rarities are auto-migrated (Name is a total sort and was swallowing Exotic/Oddity)
- **Layout** — sets `anchoredPosition` with vanilla math (height fallback); no broken `SetUpgradePosition` reflection
- **Save** — applies in-memory criteria immediately; sticky until a vanilla sort button is used
- **Filters** — keep rarity selections across open; reliable window lookup; hide + layout in one pass
- **Trashed** — only with BatchScrapping (`IsTrashMarked` / flag `0x20`)
- **Grid ↔ list toggle** — re-applies sticky priority sort and filters after `SwitchUpgradeView` / `SetupUpgrades`
  rebuilds the pool
- Depends on SparrohUILib **1.2.2+**

## 1.1.1

### Fixes

- **Priority sort** — correctly reads the static `upgradeUIs` list, sorts only the live window range, and applies layout
  via `UpdateUpgradeOrder` / `SetUpgradePosition`
- **Filters** — no longer overwrite `upgradeUICount` with the visible count (that broke later layout); hidden items are
  compacted and positions refreshed through the game layout path
- Depends on SparrohUILib **1.2.1+** for drag-list cursor follow fixes in the sort priority editor

## 1.1.0

- Initial standalone release (split from Enhanced Upgrade Menu)
- Filter panel: rarity hiding, favorites filter, context-aware stat/property filters, and clear-all
- Customizable priority sort with drag-and-drop reordering, Save/Cancel/Reset, and persistent settings
- Optional upgrade stat reformatting (`Key: **Value**`), disabled by default
- Depends on SparrohUILib for shared UI components

## 1.0.0

- Pre-split baseline while features lived in Enhanced Upgrade Menu
- Complete upgrade filtering and sorting system
- Filter panel with rarity hiding, favorite filtering, and stat-based filtering
- Property-aware sorting with custom comparisons
- Dynamic stat filter list based on current gear context
- Custom UI integration with the gear details window
- Packaging: thunderstore.toml, LICENSE, and CHANGELOG
