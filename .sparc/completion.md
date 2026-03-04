# SPARC Completion -- AepodXpBar

> **Phase**: Completion
> **Purpose**: Verification criteria, test matrix, and ship checklist

---

## 1. Test Matrix

### Phase 1 Tests (Must Pass for Ship)

| # | Test | Steps | Expected Result | Status |
|---|------|-------|----------------|--------|
| T1 | Formatted XP at level 5 | Kill a mob | XP text shows `"180 / 500 (36.0%)"` with commas | [ ] |
| T2 | XP rate appears | Kill 10 mobs over 2 min | Rate shows non-zero XP/hr, TTL shows estimate | [ ] |
| T3 | Idle timeout | Stop killing for 60s | Rate shows "---", TTL shows "---" | [ ] |
| T4 | /xp command | Type `/xp` in chat | Full stats summary in chat log (5 lines) | [ ] |
| T5 | /xp reset | Type `/xp reset` | Session stats reset, confirmation in chat | [ ] |
| T6 | Level up | Gain enough XP to level | Level completion logged (if enabled), rate continues | [ ] |
| T7 | Ascension display | Reach level 35 | Display switches to "Ascension: X / 38,000 (Y%)" | [ ] |
| T8 | Ascension point | Earn 38,000 Ascension XP | XP resets to 0, rate continues tracking | [ ] |
| T9 | Config toggle | Toggle ShowPercentage off in F1 | Percentage disappears immediately | [ ] |
| T10 | Mod disable | Toggle ModEnabled off | Vanilla XP text format restores | [ ] |
| T11 | XP bonus | Kill mob with XP bonus gear | Delta tracking captures bonus XP correctly | [ ] |
| T12 | XP Lock | Enable `/explock` | Rate shows "---", no tracking during lock | [ ] |
| T18 | ErenshorQoL compat | Install both mods | Both `/xp` and QoL commands work | [ ] |

### Phase 1 Additional Tests (from QA review)

| # | Test | Steps | Expected Result | Status |
|---|------|-------|----------------|--------|
| T19 | Load save at 35 | Load a level 35 save | Ascension format displays immediately | [ ] |
| T20 | Debug XP command | Use QoL `/add50xp` at level 1 | Delta tracked correctly, one level-up | [ ] |
| T21 | Zone transition | Zone to new area | `ForceXPBarUpdate` triggers, text re-formats | [ ] |
| T22 | Disable+re-enable | Toggle mod off then on | Rate resets, text regenerates cleanly | [ ] |
| T23 | Near-full XP bar | Get to 99.99% XP | Text fits without overflow or layout break | [ ] |
| T24 | Death and respawn | Die and respawn | Session tracking continues, no null refs | [ ] |

### Phase 1.5 Tests

| # | Test | Steps | Expected Result | Status |
|---|------|-------|----------------|--------|
| T13 | Flash effect | Kill mob with flash enabled | Text briefly flashes yellow then returns | [ ] |

### Phase 2 Tests

| # | Test | Steps | Expected Result | Status |
|---|------|-------|----------------|--------|
| T14 | Group tooltip | Hover group member name | XP tooltip appears after 0.3s delay | [ ] |
| T15 | Tooltip at 35 | Hover member at level 35 | Tooltip shows Ascension XP format | [ ] |
| T16 | Empty group slot | Hover empty group slot | No tooltip appears (null safety) | [ ] |
| T17 | Zone XP tracking | Change zones while grinding | Zone XP rate tracking resets | [ ] |
| T25 | Member departs | Hover, then member leaves group | Tooltip hides automatically | [ ] |

## 2. Performance Verification

| Metric | Target | Method |
|--------|--------|--------|
| FixedUpdate Postfix | < 0.1ms / frame | Debug timing toggle: `Stopwatch` when enabled |
| String allocations | ~1/sec (with caching) | `GC.GetTotalMemory` sampled every 5s |
| Memory (static) | < 10 KB | Manual audit of allocation sizes |
| Rate stability | Within 10% of true rate after 60s | Compare calculated rate to manual XP/time check |

## 3. Build Verification

```bash
# Build
cd AepodXpBar
dotnet build AepodXpBar.csproj \
  -p:GamePath="$ErenshorGamePath" \
  -p:BepInExPath="$ErenshorGamePath/BepInEx/core" \
  -p:CorlibPath="$ErenshorGamePath/Erenshor_Data/Managed"

# Verify DLL exists
ls -la bin/Debug/netstandard2.1/AepodXpBar.dll

# Deploy
cp bin/Debug/netstandard2.1/AepodXpBar.dll "$ErenshorGamePath/BepInEx/plugins/" && sync

# Verify in-game
# 1. Launch Erenshor
# 2. Check BepInEx log for "AepodXpBar loaded" (no errors)
# 3. Check config file exists: BepInEx/config/com.aepod.erenshor.xpbar.cfg
# 4. XP bar shows enhanced format
```

## 4. Ship Checklist

### Code Quality
- [ ] All files compile without warnings
- [ ] No `using` for unused namespaces
- [ ] All public API has XML doc comments
- [ ] No hardcoded strings (all config-driven)
- [ ] Harmony patches use correct attributes and types

### Expert Review Fixes Verified
- [ ] Braces on abbreviation branch (all 3 reviewers)
- [ ] `__state` tuple pattern in EarnedXP (not static fields)
- [ ] String caching in FixedUpdate (1s rate refresh)
- [ ] `raycastTarget = true` (not BoxCollider2D)
- [ ] Null guards before try-catch
- [ ] Rate-limited error logging
- [ ] Robust `/xp` command parsing (whitespace split)
- [ ] `float now` parameters on XPRateCalculator
- [ ] Version compatibility check in Awake
- [ ] `OnDestroy` unpatches Harmony

### Compatibility
- [ ] Works standalone (no other mods)
- [ ] Works with ErenshorQoL installed
- [ ] `/xp` and QoL commands do not conflict
- [ ] Config file generates cleanly on first run

### Documentation
- [ ] README.md with mod description, features, config reference
- [ ] Installation instructions (BepInEx plugin folder)
- [ ] Known limitations documented

## 5. Deliverables

| Artifact | Path | Description |
|----------|------|-------------|
| Mod DLL | `AepodXpBar/bin/Debug/netstandard2.1/AepodXpBar.dll` | Compiled mod |
| Config | `BepInEx/config/com.aepod.erenshor.xpbar.cfg` | Auto-generated on first run |
| BRD | `ideas/xp-numbers-bar/business-requirements.md` | v1.1 |
| Architecture | `ideas/xp-numbers-bar/technical-architecture.md` | v1.1 (reviewed) |
| Review | `ideas/xp-numbers-bar/architecture-review.md` | Expert review archive |
| SPARC Plan | `AepodXpBar/.sparc/` | This directory |
