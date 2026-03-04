# SPARC Specification -- AepodXpBar

> **Phase**: Specification
> **BRD**: `ideas/xp-numbers-bar/business-requirements.md` v1.1
> **Architecture**: `ideas/xp-numbers-bar/technical-architecture.md` v1.1
> **Expert Review**: Approved with all fixes applied (3 reviewers, consensus reached)

---

## 1. Goal

Enhance Erenshor's existing XP bar text with formatted numbers, percentage, XP/hour rate, time-to-level estimates, session statistics, and SimPlayer XP tooltips. Display-only mod -- never writes to game state.

## 2. Functional Requirements

### Phase 1: Core (MVP Ship -- 0.5 day)

| ID | Requirement | Acceptance Criteria |
|----|-------------|---------------------|
| REQ-1 | Enhanced XP text with formatted numbers + percentage | `"12,450 / 15,000 (83.0%)"` displays on XP bar |
| REQ-2 | XP/hour rate via rolling window | Non-zero rate shows after 2+ kills; "---" when idle >60s |
| REQ-3 | Time to level estimate | `"~45m to lvl"` shows when rate > 0 |
| REQ-4 | Session statistics tracking | Total XP, kill count, avg XP/kill accumulated correctly |
| REQ-6 | `/xp` chat command | Prints full stats summary; `/xp reset` clears session |
| REQ-7 | BepInEx config for all settings | 18 config entries, all hot-reloadable via F1 panel |

### Phase 1.5: Polish (0.5 day)

| ID | Requirement | Acceptance Criteria |
|----|-------------|---------------------|
| REQ-5 | Flash effect on XP gain | Text briefly flashes yellow, configurable color + duration |
| REQ-5.4 | XP bar hover tooltip | Shows full stats breakdown on mouse hover |
| REQ-11 | SimPlayer XP tooltip | Hover group member name shows their XP progress |

### Phase 2: Extended (0.75 day)

| ID | Requirement | Acceptance Criteria |
|----|-------------|---------------------|
| REQ-8 | Level-up log | Chat message with time + XP at previous level |
| REQ-9 | Per-zone rate comparison | `/xp zone` shows zone vs session rate |
| REQ-10 | Group XP efficiency | Shows XP penalty multiplier and solo-equivalent rate |


## 3. Non-Functional Requirements

| Constraint | Target |
|-----------|--------|
| Performance | FixedUpdate Postfix < 0.1ms/frame; ~1 string allocation/sec with caching |
| Memory | < 10 KB total static allocation |
| Compatibility | ErenshorQoL, ErenshorREL, COOP -- no conflicts |
| Fail-safe | Any exception falls back to vanilla text; rate-limited error logging |
| Thread safety | All code on Unity main thread; no synchronization needed |

## 4. Technical Constraints (from Decompiled Source)

- `LiveStatUpdate.XP` is `TextMeshProUGUI` -- set text in FixedUpdate Postfix
- `LiveStatUpdate.XPAmt` is private -- used for dirty-checking, we cache independently
- `Stats.XPBonus` is private -- accessed via `AccessTools.Field` cached `FieldInfo`
- `Stats.EarnedXP(int)` mutates `_incomingXP` param locally (line 603) -- Harmony Postfix receives original value (by-value semantics)
- `DoLevelUp()` fires once per `EarnedXP` call, no loop, overflow XP lost
- Level 34→35 transition sets `CurrentExperience = ExperienceToLevelUp` (not 0)
- `TypeText.CheckCommands()` is private -- must use string-based patch target
- Group window uses `GraphicRaycaster` -- tooltip needs `raycastTarget = true`, not `BoxCollider2D`

## 5. Out of Scope

- Replacing or restructuring the vanilla XP bar UI layout
- Persisting XP rate history across sessions
- Modifying XP gain amounts
- Tracking XP by ability or damage source
- Mobile/controller UI optimization
