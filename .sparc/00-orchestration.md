# SPARC Orchestration Plan -- AepodXpBar

> **Purpose**: Step-by-step build orchestration across phases
> **SPARC Plan**: `AepodXpBar/.sparc/`
> **Architecture**: `ideas/xp-numbers-bar/technical-architecture.md` v1.1 (expert-reviewed)
> **BRD**: `ideas/xp-numbers-bar/business-requirements.md` v1.1

---

## Overview

AepodXpBar is a BepInEx mod that enhances Erenshor's XP bar with formatted numbers, percentage, XP/hour rate, time-to-level estimates, session statistics, and `/xp` chat commands. It is a display-only mod that never writes to game state.

**Build target**: `netstandard2.1` DLL (`AepodXpBar.dll`)
**Deploy target**: `$ErenshorGamePath/BepInEx/plugins/AepodXpBar.dll`
**Config output**: `BepInEx/config/com.aepod.erenshor.xpbar.cfg` (auto-generated)

## Phase Map

| Phase | Scope | Deliverables | Detail |
|-------|-------|-------------|--------|
| **Phase 1** | Core MVP | Enhanced XP text, rate, TTL, `/xp` command, config | [`01-phase-1.md`](01-phase-1.md) |
| **Phase 1.5** | Polish | Flash effect, SimPlayer XP tooltip | [`02-phase-1.5.md`](02-phase-1.5.md) |
| **Phase 2** | Extended | Zone tracking, level-up log, group XP efficiency | Deferred until Phase 1 ships |

## Build & Deploy Scripts

| Script | Purpose |
|--------|---------|
| [`build.sh`](../build.sh) | Compile C# mod via `dotnet build` with MSBuild properties |
| [`deploy.sh`](../deploy.sh) | Copy DLL to game plugins folder, optional `--package` for zip |

## Orchestration Steps

### Step 0: Environment Setup
- Verify `$ErenshorGamePath` is set
- Verify `dotnet` SDK is available
- Verify game DLLs exist at expected paths
- Run: `./build.sh --check`

### Step 1: Phase 1 -- Core MVP
Execute tasks from [`01-phase-1.md`](01-phase-1.md) in dependency order:

```
1.1 Project Scaffold (AepodXpBar.csproj)
 |
 v
1.2 Plugin Entry + Config (Plugin.cs)
 |
 +---> 1.3 XPSessionData.cs
 |       |
 +---> 1.4 XPRateCalculator.cs
 |       |
 +---> 1.5 XPTextFormatter.cs
 |       |
 v       v
1.6 Harmony Patches (4 files)
 |
 v
1.7 Build, Deploy, Test (T1-T12, T18-T24)
```

**Gate**: All Phase 1 tests pass before proceeding.

### Step 2: Phase 1.5 -- Polish
Execute tasks from [`02-phase-1.5.md`](02-phase-1.5.md):

```
1.5.1 FlashController.cs
 |
 v
1.5.2 Wire flash into patches + test (T13)
 |
 v
1.5.3 GroupXPTooltip.cs + test (T14-T16, T25)
```

**Gate**: T13-T16, T25 pass, no regressions in T1-T12.

### Step 3: Phase 2 -- Extended Features
Deferred. Will create `03-phase-2.md` after Phase 1 ships and is validated in-game.

## Agent Strategy

### Solo Development (Recommended for Phase 1)
AepodXpBar is a focused, single-DLL mod. Tasks are sequential by dependency. Use a single `erenshor-developer` agent for implementation, with the SPARC plan files as grounding context.

### Parallel Opportunities
If using a team, these tasks can be parallelized within Phase 1:
- Tasks 1.3, 1.4, 1.5 (XPSessionData, XPRateCalculator, XPTextFormatter) are independent static classes
- Task 1.6 patches depend on all three being complete

### Context Files for Implementation
The implementing agent should read these files in order:
1. `AepodXpBar/.sparc/specification.md` -- requirements and constraints
2. `ideas/xp-numbers-bar/technical-architecture.md` -- full architecture with code
3. `AepodXpBar/.sparc/pseudocode.md` -- algorithm pseudocode
4. `AepodXpBar/.sparc/refinement.md` -- task breakdown
5. `AepodXpBar/.sparc/completion.md` -- test matrix and ship checklist

## Verification Protocol

After each task:
1. `./build.sh` -- must compile without errors (PostBuild .bat failure on WSL is expected)
2. `./deploy.sh` -- copy to game
3. Manual test per completion.md test matrix
4. Check BepInEx log for errors: `tail -20 "$ErenshorGamePath/BepInEx/LogOutput.log"`

## Risk Mitigations

| Risk | Mitigation |
|------|-----------|
| Game API changes between versions | Version compatibility check in `Plugin.Awake()` |
| FixedUpdate perf regression | String caching, 1s rate refresh timer, < 0.1ms target |
| Conflict with ErenshorQoL `/xp` | `[HarmonyBefore("brumdail.erenshor.qol")]` on command patch |
| Null refs during loading screens | Null guards before try-catch in FixedUpdate Postfix |
| Rate calculator overflow | Queue capacity 256, rolling window prune |
