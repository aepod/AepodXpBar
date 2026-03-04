# SPARC Architecture -- AepodXpBar

> **Phase**: Architecture
> **Full Technical Architecture**: `ideas/xp-numbers-bar/technical-architecture.md` v1.1
> **Expert Review Status**: Approved -- 3 reviewers, consensus reached

---

## Summary

This document links to the comprehensive technical architecture that has undergone expert review. The SPARC architecture phase is satisfied by the reviewed document.

## Architecture Document

**Primary reference**: [`ideas/xp-numbers-bar/technical-architecture.md`](../../ideas/xp-numbers-bar/technical-architecture.md) v1.1

This document contains:
- Complete vanilla system analysis (decompiled source for LiveStatUpdate, Stats, SimPlayerGrouping)
- Component architecture (Plugin, XPSessionData, XPRateCalculator, XPTextFormatter, GroupXPTooltip)
- 5 Harmony patch specifications with full code
- State management lifecycle and edge case coverage
- Performance analysis (hot path, memory, caching strategy)
- 18 BepInEx configuration entries
- Build and deploy commands
- Compatibility matrix
- Phase implementation map with line estimates

## Expert Review Archive

**Review file**: [`ideas/xp-numbers-bar/architecture-review.md`](../../ideas/xp-numbers-bar/architecture-review.md)

**Reviewers**:
1. **Systems Architect** -- Component boundaries, data flow, edge cases, open question resolution
2. **BepInEx Developer** -- Harmony patch correctness, decompiled source alignment, type verification, build config
3. **QA/Performance Specialist** -- Hot path analysis (21 issues), memory, rate accuracy, test strategy, fail-safety

**Consensus**: Architecture approved with all fixes applied in v1.1.

## Key Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Patch strategy for XP text | Postfix on `FixedUpdate` | Lets vanilla update bar fill normally; we only overwrite text |
| Delta tracking | Prefix+Postfix on `EarnedXP` with `__state` | Captures XP before/after for accurate delta, handles level-up edge case |
| Rate algorithm | Rolling window queue | Configurable window, natural warm-up, O(k) prune |
| Hot path caching | Cache formatted string, regenerate on XP change or 1s timer | 98% allocation reduction (50/sec → 1/sec) |
| Tooltip hover | `EventTrigger` + `raycastTarget = true` | Standard Unity UI raycasting, no physics colliders |
| Private field access | `AccessTools.Field` cached `FieldInfo` | Standard Harmony pattern, one-time reflection cost |
| Tooltip parenting | Under group window canvas | Auto-hides when window closes |
| Text overflow | TMP `enableAutoSizing` | Least invasive, handles all feature combinations |

## Project Structure

```
AepodXpBar/
+-- .sparc/
|   +-- specification.md        <- Requirements & constraints
|   +-- pseudocode.md           <- Algorithm pseudocode
|   +-- architecture.md         <- This file (links to full arch)
|   +-- refinement.md           <- Implementation tasks
|   +-- completion.md           <- Verification checklist
+-- AepodXpBar.csproj           <- Project file (netstandard2.1)
+-- Plugin.cs                   <- BepInEx entry, config, Harmony
+-- XPSessionData.cs            <- Static session state
+-- XPRateCalculator.cs         <- Rolling window rate
+-- XPTextFormatter.cs          <- String formatting
+-- FlashController.cs          <- Phase 1.5: XP gain flash
+-- GroupXPTooltip.cs            <- Phase 2: SimPlayer tooltip
+-- Patches/
|   +-- LiveStatUpdatePatch.cs  <- FixedUpdate Postfix
|   +-- StatsEarnedXPPatch.cs   <- EarnedXP Prefix+Postfix
|   +-- StatsDoLevelUpPatch.cs  <- DoLevelUp Postfix
|   +-- TypeTextCommandPatch.cs <- /xp command Prefix
```
