# Phase 1: Core MVP -- AepodXpBar

> **Target**: Complete working mod with enhanced XP display, rate tracking, and `/xp` command
> **Architecture**: `ideas/xp-numbers-bar/technical-architecture.md` v1.1
> **Pseudocode**: `AepodXpBar/.sparc/pseudocode.md`
> **Tests**: T1-T12, T18-T24 from `completion.md`

---

## Task 1.1: Project Scaffold

**Create**: `AepodXpBar/AepodXpBar.csproj`

### Steps
1. Create `AepodXpBar.csproj` with `netstandard2.1` target
2. Add DLL references with `$(GamePath)`, `$(BepInExPath)`, `$(CorlibPath)` MSBuild properties:
   - `$(CorlibPath)/Assembly-CSharp.dll`
   - `$(CorlibPath)/UnityEngine.dll`
   - `$(CorlibPath)/UnityEngine.CoreModule.dll`
   - `$(CorlibPath)/UnityEngine.UI.dll`
   - `$(CorlibPath)/Unity.TextMeshPro.dll`
   - `$(BepInExPath)/BepInEx.dll`
   - `$(BepInExPath)/0Harmony.dll`
3. Set `<Private>false</Private>` on all references
4. Add `<LangVersion>latest</LangVersion>` for tuple support

### Verify
```bash
./build.sh
# DLL exists at bin/Debug/netstandard2.1/AepodXpBar.dll
# PostBuild .bat failure under WSL is expected and harmless
```

### Reference
- Architecture Section 11 (Build & Deploy)
- `ErenshorLLM/mod/ErenshorLLMDialog.csproj` for pattern

---

## Task 1.2: Plugin Entry + Config

**Create**: `AepodXpBar/Plugin.cs`

### Steps
1. `[BepInPlugin("com.aepod.erenshor.xpbar", "AepodXpBar", "1.0.0")]`
2. Inherit `BaseUnityPlugin`
3. Bind 18 `ConfigEntry` fields across 5 categories:
   - **General**: `ModEnabled` (bool, true)
   - **Display**: `FormatNumbers` (bool, true), `AbbreviateNumbers` (bool, false), `ShowPercentage` (bool, true), `PercentageDecimals` (int, 1), `ShowRate` (bool, true), `ShowTimeToLevel` (bool, true)
   - **Rate**: `RollingWindowMinutes` (float, 5.0), `IdleTimeoutSeconds` (float, 60.0)
   - **Commands**: `EnableXPCommand` (bool, true), `LogLevelCompletion` (bool, false)
   - **Group**: `EnableGroupTooltip` (bool, true), `TooltipDelay` (float, 0.3)
   - **Flash**: `EnableFlash` (bool, true), `FlashDuration` (float, 0.4), `FlashColor` (string, "#FFD700")
   - Plus any remaining from architecture Section 8
4. Cache `FieldInfo` for `Stats.XPBonus` via `AccessTools.Field(typeof(Stats), "XPBonus")`
5. Version compatibility check: verify `LiveStatUpdate.FixedUpdate` exists via `AccessTools.Method`
6. Apply Harmony patches: `_harmony = new Harmony("com.aepod.erenshor.xpbar"); _harmony.PatchAll()`
7. Wire `SettingChanged` handlers:
   - `ModEnabled`: reset rate calculator + invalidate cache on re-enable
   - `RollingWindowMinutes`: retroactive prune via `PruneAndRecalculate`
8. `OnDestroy()`: `_harmony.UnpatchSelf()`, null out static refs

### Key Code Pattern (from pseudocode Section 1)
```csharp
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "com.aepod.erenshor.xpbar";
    // ... ConfigEntry fields ...
    // ... static FieldInfo XPBonusField ...
    private Harmony _harmony;

    void Awake()
    {
        // Bind config
        // Cache reflection
        // Verify compatibility
        // Patch
    }

    void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
```

### Verify
```bash
./build.sh && ./deploy.sh
# Launch Erenshor
# Check: BepInEx log shows "AepodXpBar loaded" (no errors)
# Check: BepInEx/config/com.aepod.erenshor.xpbar.cfg exists with all entries
```

---

## Task 1.3: XPSessionData

**Create**: `AepodXpBar/XPSessionData.cs`

### Steps
1. Static class with fields:
   - `SessionStartTime` (float)
   - `SessionTotalXP` (int)
   - `SessionKillCount` (int)
   - `LastXPGainTime` (float)
   - `SessionStarted` (bool)
   - `LastKnownLevel` (int)
   - `LevelStartTime` (float)
   - `LevelTotalXP` (int)
2. `RecordGain(int delta, float now)`: accumulate totals, set `SessionStarted = true` on first call, update `LastXPGainTime`
3. `OnLevelUp(int newLevel)`: log completion if configured, reset `LevelStartTime` + `LevelTotalXP`, update `LastKnownLevel`
4. `Reset()`: clear all fields back to defaults

### Reference
- Pseudocode Section 2 (XP Gain Tracking)
- Architecture Section 3.2 (XPSessionData)

---

## Task 1.4: XPRateCalculator

**Create**: `AepodXpBar/XPRateCalculator.cs`

### Steps
1. Static class with:
   - `Queue<(float time, int xp)>` capacity 256
   - `float cachedRate`
   - `float windowMinutes` (from config, default 5.0)
   - `float idleTimeoutSeconds` (from config, default 60.0)
2. `RecordXPGain(int amount, float now)`: enqueue, prune, recalculate
3. `PruneExpired(float now)`: dequeue entries older than `windowMinutes * 60`
4. `RecalculateRate(float now)`: sum / duration * 3600, guard `count < 2`
5. `GetXPPerHour(float now)`: return 0 if idle (check `LastXPGainTime` vs `idleTimeoutSeconds`), else `cachedRate`
6. `GetTimeToLevelSeconds(int currentXP, int neededXP, float now)`: `remaining / rate * 3600`, return -1 if rate <= 0
7. `PruneAndRecalculate(float now)`: public method for timer-based decay (called from FixedUpdate 1s timer)
8. `Reset()`: clear queue and cached rate

### Critical: All methods accept `float now` parameter (not `Time.time`) for testability.

### Reference
- Pseudocode Section 3 (Rolling Window)
- Architecture Section 3.3 (XPRateCalculator)

---

## Task 1.5: XPTextFormatter

**Create**: `AepodXpBar/XPTextFormatter.cs`

### Steps
1. Static class with pre-allocated `StringBuilder(128)`
2. `FormatXPText(Stats stats)` builds string per config:
   - **Ascension check**: `stats.Level >= 35` uses `CurrentAscensionXP` / `AscensionXPtoLevelUp`
   - **Normal**: `CurrentExperience` / `ExperienceToLevelUp`
   - **Number format**: `"N0"` (commas), abbreviated (`12.5k`), or raw
   - **Percentage**: `(83.0%)` with configurable decimal places
   - **Rate**: `12,450 XP/hr` or `--- XP/hr`
   - **Time to level**: `~45m` or `~1h 23m` or `---`
3. `AppendAbbreviated(StringBuilder sb, int value)`: manual int-to-abbreviated string (no boxing)
   - `< 1000` → raw
   - `< 100_000` → `"12.5k"`
   - `< 1_000_000` → `"123k"`
   - `>= 1_000_000` → `"1.2M"`
   - **Must have braces on this branch** (expert review fix)
4. `AppendTimeShort(StringBuilder sb, float seconds)`: `~45m` / `~1h 23m` / `~3h+`
5. `FormatTimeLong(float seconds)`: for `/xp` command output (`"1 hour, 23 minutes"`)
6. Cache percentage format string based on `PercentageDecimals` config

### Reference
- Pseudocode Section 4 (XP Text Formatting)
- Architecture Section 3.4 (XPTextFormatter)

---

## Task 1.6: Harmony Patches

**Create**: 4 files in `AepodXpBar/Patches/`

### 1.6.1: `LiveStatUpdatePatch.cs`
- `[HarmonyPatch(typeof(LiveStatUpdate), "FixedUpdate")]` Postfix
- Caching state: `_lastXP`, `_lastLevel`, `_cached` (string), `_nextRateRefresh` (float)
- Null guards: `instance.PlayerStats == null || instance.XP == null` → return
- XP change detection: `curXP != _lastXP || stats.Level != _lastLevel`
- Rate staleness: `now >= _nextRateRefresh`
- If changed or stale: regenerate via `XPTextFormatter.FormatXPText(stats)`, update `instance.XP.text`
- If stale: call `XPRateCalculator.PruneAndRecalculate(now)` for natural decay
- Rate-limited error logging: `Time.frameCount % 500 == 0`
- `InvalidateCache()` static method (called on mod toggle)

### 1.6.2: `StatsEarnedXPPatch.cs`
- `[HarmonyPatch(typeof(Stats), "EarnedXP")]` Prefix + Postfix
- **Prefix**: capture `__state = (xp, level)` via `out (int xp, int level) __state`
  - Filter `__instance.Myself.isNPC`
  - Read `CurrentExperience` or `CurrentAscensionXP` based on level
- **Postfix**: compute delta, record gain
  - Filter `isNPC` and `GameData.XPLock == 1`
  - Delta: `xpAfter - state.xp` (normal), `CalculateActualGain` (level-up/ascension reset)
  - Call `XPSessionData.RecordGain(delta, now)` and `XPRateCalculator.RecordXPGain(delta, now)`
- `CalculateActualGain(Stats stats, int incomingXP)`: use cached `XPBonusField.GetValue(stats)`

### 1.6.3: `StatsDoLevelUpPatch.cs`
- `[HarmonyPatch(typeof(Stats), "DoLevelUp")]` Postfix
- Filter `__instance.Myself.isNPC`
- Optional level completion log (gated by `LogLevelCompletion` config)
- Call `XPSessionData.OnLevelUp(__instance.Level)`

### 1.6.4: `TypeTextCommandPatch.cs`
- `[HarmonyPatch(typeof(TypeText), "CheckCommands")]` Prefix
- `[HarmonyBefore("brumdail.erenshor.qol")]` for ErenshorQoL compatibility
- Parse: `input.StartsWith("/xp")` then `Split(' ', StringSplitOptions.RemoveEmptyEntries)`
- Handle: `/xp` (print stats), `/xp reset` (clear session), `/xp zone` (Phase 2 stub)
- `PrintXPStats()`: 5 chat log lines using `GameData.AddToChatLog()`
  - Line 1: `[XP Stats] Level {level} {class}` (gold color)
  - Line 2: `  Current: {cur:N0} / {need:N0} ({pct:F1}%)` (white)
  - Line 3: `  Rate: {rate} XP/hr ({window}m window)` (white)
  - Line 4: `  Time to level: {ttl}` (white)
  - Line 5: `  Session: {totalXP:N0} XP over {kills} kills (avg {avg:F1}/kill)` (white)
- Return `false` to consume command, `true` to pass through

### Reference
- Pseudocode Sections 2, 5, 6, 8
- Architecture Sections 4.1-4.5
- Expert review: `__state` pattern, null guards, rate-limited logging, `HarmonyBefore`

---

## Task 1.7: Build, Deploy, Test

### Steps
1. Build: `./build.sh`
2. Deploy: `./deploy.sh`
3. Launch Erenshor
4. Run test matrix from `completion.md`:

| Test | Action | Expected |
|------|--------|----------|
| T1 | Kill mob at level 5 | XP shows `"180 / 500 (36.0%)"` |
| T2 | Kill 10 mobs over 2 min | Rate shows non-zero XP/hr, TTL shows estimate |
| T3 | Stop killing for 60s | Rate shows `"---"`, TTL shows `"---"` |
| T4 | Type `/xp` in chat | 5-line stats summary in chat log |
| T5 | Type `/xp reset` | Session stats reset, confirmation |
| T6 | Level up | Level completion logged if enabled, rate continues |
| T7 | Reach level 35 | Display switches to Ascension format |
| T8 | Earn 38,000 Ascension XP | XP resets, rate continues |
| T9 | Toggle ShowPercentage off (F1) | Percentage disappears immediately |
| T10 | Toggle ModEnabled off | Vanilla XP text restores |
| T11 | Kill mob with XP bonus gear | Delta tracking captures bonus correctly |
| T12 | Enable `/explock` | Rate shows `"---"`, no tracking during lock |
| T18 | Install with ErenshorQoL | Both `/xp` and QoL commands work |
| T19 | Load save at level 35 | Ascension format displays immediately |
| T20 | Use QoL `/add50xp` at level 1 | Delta tracked correctly |
| T21 | Zone transition | `ForceXPBarUpdate` triggers, text re-formats |
| T22 | Disable + re-enable mod | Rate resets, text regenerates |
| T23 | Near-full XP bar (99.99%) | Text fits without overflow |
| T24 | Death and respawn | Session continues, no null refs |

### Gate
All tests pass → Phase 1 complete. Proceed to Phase 1.5.
