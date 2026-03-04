# AepodXpBar

A BepInEx mod for [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/) that enhances the XP bar with detailed progress tracking, rate statistics, and group member tooltips.

## Features

### XP Bar Enhancement
- **Detailed XP display** -- Shows current / needed XP with optional percentage
- **XP per hour rate** -- Rolling window calculation so you know your grind speed
- **Time to level estimate** -- How long until your next ding at current pace
- **Number formatting** -- Thousands separators (12,450) or abbreviations (12.5k)
- **Configurable decimals** -- 0-2 decimal places on percentage display

### XP Gain Flash
- Brief color flash on the XP text when you gain experience
- Configurable flash color and duration

### Group Member XP Tooltip
- Hover over any group member row in the party window to see their XP progress
- Shows name, level, class, XP bar, and level comparison to you
- Follows your mouse cursor with smart screen-edge positioning
- Configurable hover delay

### /xp Chat Command
- `/xp` -- Session statistics (total XP, kills, XP/hr, time played)
- `/xp reset` -- Reset session tracking

### Level Completion Logging
- Optional chat message when you complete a level with time and XP stats

## Installation

Requires [BepInEx 5.4.x](https://docs.bepinex.dev/articles/user_guide/installation/index.html) for Unity Mono.

1. Drop `AepodXpBar.dll` into your `BepInEx/plugins/` folder
2. Launch the game -- a config file will be generated at `BepInEx/config/com.aepod.erenshor.xpbar.cfg`

## Configuration

All settings are in `BepInEx/config/com.aepod.erenshor.xpbar.cfg` (or use BepInEx ConfigManager).

| Section | Setting | Default | Description |
|---------|---------|---------|-------------|
| General | ModEnabled | true | Master toggle |
| Display | FormatNumbers | true | Thousands separators |
| Display | AbbreviateNumbers | false | Abbreviate large numbers (12.5k) |
| Display | ShowPercentage | true | Show % on XP bar |
| Display | PercentageDecimals | 1 | Decimal places (0-2) |
| Display | ShowRate | true | Show XP/hr |
| Display | ShowTimeToLevel | true | Show time estimate |
| Rate | RollingWindowMinutes | 5 | Rate calculation window (1-60) |
| Rate | IdleTimeoutSeconds | 60 | Seconds before rate shows idle |
| Commands | EnableXPCommand | true | Enable /xp command |
| Commands | LogLevelCompletion | false | Chat message on level-up |
| Flash | EnableFlash | true | Flash on XP gain |
| Flash | FlashDuration | 0.4 | Flash length in seconds |
| Flash | FlashColor | #FFD700 | Flash color (hex) |
| Group | EnableGroupTooltip | true | Group member XP tooltip |
| Group | TooltipDelay | 0.3 | Hover delay in seconds |

## Compatibility

- Erenshor (current Steam version)
- BepInEx 5.4.x (Unity Mono)
- Compatible with ErenshorQoL (`/xp` command uses `[HarmonyBefore]` to avoid conflicts)

## Building from Source

```bash
export ErenshorGamePath="/path/to/Erenshor"
dotnet build AepodXpBar.csproj \
  -p:GamePath="$ErenshorGamePath" \
  -p:BepInExPath="$ErenshorGamePath/BepInEx/core" \
  -p:CorlibPath="$ErenshorGamePath/Erenshor_Data/Managed"
```

## Source

https://github.com/aepod/AepodXpBar

## License

MIT
