# SPARC Refinement -- AepodXpBar

> **Phase**: Refinement
> **Purpose**: Implementation task breakdown with ordering, dependencies, and verification

---

## Implementation Order

### Phase 1: Core MVP (Target: 0.5 day)

Tasks are ordered by dependency -- each builds on the previous.

#### Task 1.1: Project Scaffold
**Files**: `AepodXpBar.csproj`
**Actions**:
- Create .csproj with netstandard2.1 target
- Add all DLL references (Assembly-CSharp, UnityEngine, UnityEngine.CoreModule, UnityEngine.UI, Unity.TextMeshPro, BepInEx, 0Harmony)
- Set `<Private>false</Private>` on all references
- Use MSBuild properties for paths: `$(GamePath)`, `$(BepInExPath)`, `$(CorlibPath)`
- Verify: `dotnet build` produces AepodXpBar.dll without errors (PostBuild .bat failure under WSL is expected)

#### Task 1.2: Plugin Entry + Config
**Files**: `Plugin.cs`
**Actions**:
- BepInPlugin attribute with GUID `com.aepod.erenshor.xpbar`
- Bind 18 ConfigEntry fields across 5 categories (General, Display, Rate, Commands, Group)
- Cache `FieldInfo` for `Stats.XPBonus` via `AccessTools.Field`
- Version compatibility check (verify `LiveStatUpdate.FixedUpdate` exists)
- Apply Harmony patches via `PatchAll()`
- Wire `SettingChanged` handlers for `ModEnabled` (cache invalidation + rate reset) and `RollingWindowMinutes` (retroactive prune)
- `OnDestroy()`: `_harmony.UnpatchSelf()`, null tooltip ref
- Verify: Plugin loads in BepInEx, config file appears at `BepInEx/config/com.aepod.erenshor.xpbar.cfg`

#### Task 1.3: XPSessionData
**Files**: `XPSessionData.cs`
**Actions**:
- Static class with session state fields (SessionStartTime, SessionTotalXP, SessionKillCount, LastXPGainTime, SessionStarted, LastKnownLevel, LevelStartTime, LevelTotalXP)
- `RecordGain(int delta, float now)`: accumulate totals, set SessionStarted on first call
- `OnLevelUp(int newLevel)`: log completion (optional), reset LevelStartTime + LevelTotalXP
- `Reset()`: clear all state
- Verify: Fields update correctly when called from test harness

#### Task 1.4: XPRateCalculator
**Files**: `XPRateCalculator.cs`
**Actions**:
- Static class with `Queue<(float, int)>` capacity 256
- `RecordXPGain(int amount, float now)`: enqueue, prune, recalculate
- `PruneExpired(float now)`: dequeue entries older than window
- `RecalculateRate(float now)`: sum XP / duration * 3600, guard count < 2
- `GetXPPerHour(float now)`: return cached rate or 0 if idle
- `GetTimeToLevelSeconds(int cur, int need, float now)`: remaining / rate * 3600
- `PruneAndRecalculate(float now)`: public method for timer-based decay
- `Reset()`: clear queue and rate
- All methods accept `float now` for testability
- Verify: Unit-testable: feed known (time, xp) pairs, assert rate

#### Task 1.5: XPTextFormatter
**Files**: `XPTextFormatter.cs`
**Actions**:
- Static class with pre-allocated `StringBuilder(128)`
- `FormatXPText(Stats stats)`: build formatted string per config
- Handle normal XP (Level < 35) and Ascension XP (Level >= 35)
- Number formatting: `"N0"` (commas), abbreviated (`12.5k`), or raw
- Percentage with configurable decimal places (cached format string)
- Rate display with "---" for zero rate
- Time-to-level with adaptive format (`~45m` or `~1h 23m`)
- `AppendTimeShort(StringBuilder, float seconds)`: short time format
- `AppendAbbreviated(StringBuilder, int value)`: manual int→string (no boxing)
- `FormatTimeLong(float seconds)`: for `/xp` command output
- Verify: Compile and test with known Stats values

#### Task 1.6: Harmony Patches
**Files**: `Patches/LiveStatUpdatePatch.cs`, `Patches/StatsEarnedXPPatch.cs`, `Patches/StatsDoLevelUpPatch.cs`, `Patches/TypeTextCommandPatch.cs`
**Actions**:

**LiveStatUpdatePatch.cs**:
- Postfix on `LiveStatUpdate.FixedUpdate()`
- Caching: `_lastXP`, `_lastLevel`, `_cached`, `_nextRateRefresh`
- Null guards: `PlayerStats == null || XP == null` → return
- Rate refresh: call `PruneAndRecalculate` on 1s timer
- Rate-limited error logging (every 500 frames)
- `InvalidateCache()` static method

**StatsEarnedXPPatch.cs**:
- Prefix+Postfix on `Stats.EarnedXP(int)`
- Use `__state` tuple `(int xp, int level)` for Prefix→Postfix data passing
- Filter `isNPC` in both Prefix and Postfix
- Check `GameData.XPLock == 1` in Postfix
- Delta: normal case `xpAfter - state.xp`, level-up/Ascension fallback via `CalculateActualGain`
- Document: Harmony Postfix receives original `_incomingXP` (by-value semantics)

**StatsDoLevelUpPatch.cs**:
- Postfix on `Stats.DoLevelUp()`
- Filter `isNPC`
- Optional level completion log (REQ-8, gated by config)
- Call `XPSessionData.OnLevelUp()`

**TypeTextCommandPatch.cs**:
- Prefix on `TypeText.CheckCommands` (string target, private method)
- `[HarmonyBefore("brumdail.erenshor.qol")]`
- Parse with `StartsWith("/xp")` + `Split(' ', RemoveEmpty)`
- Handle `/xp`, `/xp reset`, `/xp zone` (Phase 2)
- `PrintXPStats()`: 5 chat log lines with gold/white colors
- Return `true` for non-matching commands (pass through)

#### Task 1.7: Build, Deploy, Test
**Actions**:
- Build: `dotnet build AepodXpBar.csproj -p:GamePath=... -p:BepInExPath=... -p:CorlibPath=...`
- Deploy: `cp bin/Debug/netstandard2.1/AepodXpBar.dll "$ErenshorGamePath/BepInEx/plugins/"`
- Test cases T1-T12, T18 from architecture Section 12
- Performance: verify < 0.1ms FixedUpdate overhead

---

### Phase 1.5: Polish (Target: 0.5 day)

#### Task 1.5.1: Flash Controller
**Files**: `FlashController.cs`, updates to `Plugin.cs`, `LiveStatUpdatePatch.cs`
**Actions**:
- Track flash state: `IsFlashing`, `CurrentColor`, `_flashTimer`
- Trigger flash on XP gain (from `StatsEarnedXPPatch.Postfix`)
- Lerp color from flash color back to normal over configurable duration
- Apply color in `LiveStatUpdatePatch` when flashing
- Test: T13

#### Task 1.5.2: XP Bar Hover Tooltip
**Files**: Updates to `XPTextFormatter.cs` or new tooltip logic
**Actions**:
- Detect mouse hover over XP bar (similar EventTrigger approach)
- Show full stats breakdown as tooltip
- Test: manual hover verification

---

### Phase 2: Extended Features (Target: 0.75 day)

#### Task 2.1: Level-Up Log (REQ-8)
**Files**: Updates to `StatsDoLevelUpPatch.cs`, `XPSessionData.cs`
- Uncomment/enable level completion logging
- Test: T6 (verify log output on level-up)

#### Task 2.2: Zone Rate Tracking (REQ-9)
**Files**: Updates to `XPSessionData.cs`, `TypeTextCommandPatch.cs`
- Add `CurrentZone`, `ZoneStartTime`, `ZoneTotalXP` fields
- Hook `ZoneAnnounce` for zone change detection
- Implement `/xp zone` command
- Test: T17

#### Task 2.3: Group XP Efficiency (REQ-10)
**Files**: Updates to `TypeTextCommandPatch.cs` or `XPTextFormatter.cs`
- Read group size from `GameData.GroupMembers`
- Calculate XP penalty multiplier
- Display solo-equivalent rate
- Test: form group, verify efficiency display

#### Task 2.4: SimPlayer XP Tooltip (REQ-11)
**Files**: `GroupXPTooltip.cs`, updates to `Plugin.cs`
**Actions**:
- Create `GroupXPTooltip` MonoBehaviour
- Init: attach to SimPlayerGrouping (Postfix on `SimPlayerGrouping.Start()` or lazy init)
- Create tooltip GameObject parented under group window canvas
- Attach EventTriggers with `raycastTarget = true` on name texts
- Show tooltip after configurable delay (default 0.3s)
- Display: name, level, class, XP progress, level comparison
- Handle null members, stale data, group changes
- Test: T14-T16

---

## Refinement Checklist

- [ ] All expert review fixes from v1.1 are reflected in implementation
- [ ] Harmony `__state` pattern used (not static fields)
- [ ] String caching in FixedUpdate Postfix (1s rate refresh)
- [ ] `raycastTarget = true` for tooltip (no BoxCollider2D)
- [ ] Null guards in all patch entry points
- [ ] Rate-limited error logging
- [ ] `PruneAndRecalculate` for natural rate decay
- [ ] Config GUID aligned: `com.aepod.erenshor.xpbar`
- [ ] `AccessTools.Field` cached once in Awake
- [ ] Version compatibility check before patching
- [ ] `OnDestroy` unpatches Harmony
- [ ] All `XPRateCalculator` methods accept `float now`
