# SPARC Pseudocode -- AepodXpBar

> **Phase**: Pseudocode
> **Architecture Reference**: `ideas/xp-numbers-bar/technical-architecture.md` v1.1

---

## 1. Plugin Initialization

```
FUNCTION Plugin.Awake():
    // Config
    BIND 18 ConfigEntry fields (Display, Rate, Commands, Group categories)

    // Cached reflection
    XPBonusField = AccessTools.Field(typeof(Stats), "XPBonus")
    IF XPBonusField IS NULL:
        LOG_ERROR "Cannot find Stats.XPBonus field"
        RETURN

    // Version compatibility
    IF AccessTools.Method(typeof(LiveStatUpdate), "FixedUpdate") IS NULL:
        LOG_ERROR "Game version incompatible"
        RETURN

    // Harmony
    _harmony = new Harmony("com.aepod.erenshor.xpbar")
    _harmony.PatchAll()

    // Config change handlers
    ModEnabled.SettingChanged += ON_MOD_TOGGLE
    RollingWindowMinutes.SettingChanged += ON_WINDOW_CHANGE

FUNCTION ON_MOD_TOGGLE():
    IF ModEnabled transitions FALSE -> TRUE:
        XPRateCalculator.Reset()  // Clear stale rate data
        LiveStatUpdatePatch.InvalidateCache()  // Force text regeneration

FUNCTION ON_WINDOW_CHANGE():
    XPRateCalculator.WindowMinutes = RollingWindowMinutes.Value
    XPRateCalculator.PruneAndRecalculate(Time.time)  // Retroactive prune
```

## 2. XP Gain Tracking (Stats.EarnedXP Prefix+Postfix)

```
FUNCTION Prefix(stats, OUT state):
    IF stats.Myself.isNPC:
        state = DEFAULT
        RETURN

    IF stats.Level < 35:
        state.xp = stats.CurrentExperience
    ELSE:
        state.xp = stats.CurrentAscensionXP
    state.level = stats.Level

FUNCTION Postfix(stats, incomingXP, state):
    IF stats.Myself.isNPC: RETURN
    IF GameData.XPLock == 1: RETURN

    // Read post-call state
    IF stats.Level < 35:
        xpAfter = stats.CurrentExperience
    ELSE:
        xpAfter = stats.CurrentAscensionXP
    levelAfter = stats.Level

    // Compute delta
    IF levelAfter > state.level:
        // Level-up occurred: XP was reset. Use CalculateActualGain.
        delta = CalculateActualGain(stats, incomingXP)
    ELSE IF xpAfter < state.xp AND stats.Level >= 35:
        // Ascension point gained: XP was reset to 0
        delta = CalculateActualGain(stats, incomingXP)
    ELSE:
        delta = xpAfter - state.xp

    IF delta > 0:
        now = Time.time
        XPSessionData.RecordGain(delta, now)
        XPRateCalculator.RecordXPGain(delta, now)

FUNCTION CalculateActualGain(stats, incomingXP):
    bonus = (float)XPBonusField.GetValue(stats)
    bonusXP = RoundToInt(incomingXP * bonus)
    RETURN incomingXP + bonusXP
```

## 3. XP Rate Calculator (Rolling Window)

```
STATE:
    window: Queue<(time: float, xp: int)> with capacity 256
    cachedRate: float = 0
    windowMinutes: float = 5.0
    idleTimeoutSeconds: float = 60.0

FUNCTION RecordXPGain(amount, now):
    window.Enqueue((now, amount))
    PruneExpired(now)
    RecalculateRate(now)

FUNCTION PruneExpired(now):
    cutoff = now - (windowMinutes * 60)
    WHILE window.Count > 0 AND window.Peek().time < cutoff:
        window.Dequeue()

FUNCTION RecalculateRate(now):
    IF window.Count < 2:
        cachedRate = 0
        RETURN
    totalXP = SUM of all window[].xp
    duration = now - window.Peek().time
    IF duration > 0.5:
        cachedRate = totalXP / duration * 3600
    ELSE:
        cachedRate = 0

FUNCTION GetXPPerHour(now):
    IF now - XPSessionData.LastXPGainTime > idleTimeoutSeconds:
        RETURN 0
    RETURN cachedRate

FUNCTION GetTimeToLevelSeconds(currentXP, neededXP, now):
    rate = GetXPPerHour(now)
    IF rate <= 0: RETURN -1
    remaining = neededXP - currentXP
    RETURN remaining / rate * 3600
```

## 4. XP Text Formatting

```
STATE:
    sb: StringBuilder(128) -- pre-allocated, reused

FUNCTION FormatXPText(stats):
    sb.Clear()

    isAscension = stats.Level >= 35
    IF isAscension:
        current = stats.CurrentAscensionXP
        needed = stats.AscensionXPtoLevelUp
        sb.Append("Ascension: ")
    ELSE:
        current = stats.CurrentExperience
        needed = stats.ExperienceToLevelUp

    // Numbers
    IF config.FormatNumbers:
        sb.Append(current.ToString("N0")).Append(" / ").Append(needed.ToString("N0"))
    ELSE IF config.AbbreviateNumbers:
        AppendAbbreviated(sb, current)
        sb.Append(" / ")
        AppendAbbreviated(sb, needed)
    ELSE:
        sb.Append(current).Append(" / ").Append(needed)

    // Percentage
    IF config.ShowPercentage AND needed > 0:
        pct = (float)current / needed * 100
        sb.Append(" (").Append(pct.ToString(pctFormat)).Append("%)")

    // Rate
    now = Time.time
    rate = XPRateCalculator.GetXPPerHour(now)
    IF config.ShowRate:
        sb.Append("  |  ")
        IF rate > 0:
            sb.Append(((int)rate).ToString("N0")).Append(" XP/hr")
        ELSE:
            sb.Append("--- XP/hr")

    // Time to level
    IF config.ShowTimeToLevel:
        sb.Append("  |  ")
        ttl = XPRateCalculator.GetTimeToLevelSeconds(current, needed, now)
        IF ttl > 0:
            AppendTimeShort(sb, ttl)
        ELSE:
            sb.Append("---")

    RETURN sb.ToString()
```

## 5. FixedUpdate Display (with Caching)

```
STATE:
    lastXP: int = MIN_VALUE
    lastLevel: int = -1
    cached: string = null
    nextRateRefresh: float = 0

FUNCTION FixedUpdatePostfix(instance):
    IF NOT modEnabled: RETURN
    IF instance.PlayerStats IS NULL OR instance.XP IS NULL: RETURN

    TRY:
        stats = instance.PlayerStats
        curXP = stats.Level < 35 ? stats.CurrentExperience : stats.CurrentAscensionXP
        now = Time.time

        xpChanged = curXP != lastXP OR stats.Level != lastLevel
        rateStale = now >= nextRateRefresh

        IF xpChanged OR rateStale OR cached IS NULL:
            // On rate refresh, prune and recalc for natural decay
            IF rateStale:
                XPRateCalculator.PruneAndRecalculate(now)

            cached = XPTextFormatter.FormatXPText(stats)
            lastXP = curXP
            lastLevel = stats.Level
            IF rateStale: nextRateRefresh = now + 1.0

            instance.XP.text = cached
        // ELSE: cached text already set on TMP, skip setter
    CATCH ex:
        IF frameCount % 500 == 0:  // Rate-limit error logging
            LOG_WARNING "XP format error: " + ex.Message
```

## 6. Chat Command (/xp)

```
FUNCTION CheckCommandsPrefix(instance):
    IF NOT enableXPCommand: RETURN PASS_THROUGH

    input = instance.typed.text.Trim()
    IF NOT input.StartsWith("/xp"): RETURN PASS_THROUGH

    parts = input.Split(' ', RemoveEmpty)

    IF parts.Length == 1:  // "/xp"
        PrintXPStats()
        instance.typed.text = ""
        RETURN CONSUME

    IF parts.Length == 2 AND parts[1] == "reset":
        XPSessionData.Reset()
        XPRateCalculator.Reset()
        LOG_CHAT "[XP] Session tracking reset."
        instance.typed.text = ""
        RETURN CONSUME

    IF parts.Length == 2 AND parts[1] == "zone":
        PrintZoneStats()
        instance.typed.text = ""
        RETURN CONSUME

    RETURN PASS_THROUGH  // Unknown /xp subcommand

FUNCTION PrintXPStats():
    stats = GameData.PlayerStats
    isAsc = stats.Level >= 35

    LOG_CHAT "[XP Stats] Level {level} {class}" (gold)

    cur = isAsc ? stats.CurrentAscensionXP : stats.CurrentExperience
    need = isAsc ? stats.AscensionXPtoLevelUp : stats.ExperienceToLevelUp
    pct = (float)cur / need * 100
    LOG_CHAT "  Current: {cur:N0} / {need:N0} ({pct:F1}%)" (white)

    rate = XPRateCalculator.GetXPPerHour(Time.time)
    LOG_CHAT "  Rate: {rate} XP/hr ({window}m window)" (white)

    ttl = XPRateCalculator.GetTimeToLevelSeconds(cur, need, Time.time)
    LOG_CHAT "  Time to level: {ttl}" (white)

    avgXP = sessionTotalXP / killCount
    LOG_CHAT "  Session: {totalXP:N0} XP over {kills} kills (avg {avg:F1}/kill)" (white)
    LOG_CHAT "  Session time: {elapsed}" (white)
```

## 7. SimPlayer XP Tooltip (Phase 2)

```
FUNCTION Init(grouping):
    nameTexts = [grouping.PlayerOneName..PlayerFourName]
    CreateTooltipGameObject()  // Parent under group window canvas
    AttachEventTriggers()

FUNCTION AttachEventTriggers():
    FOR i = 0 TO 3:
        nameTexts[i].raycastTarget = true  // Enable UI raycasting (NOT BoxCollider2D)
        trigger = AddComponent<EventTrigger>(nameTexts[i].gameObject)
        slot = i  // Capture
        AddEvent(trigger, PointerEnter, () => OnSlotEnter(slot))
        AddEvent(trigger, PointerExit, () => OnSlotExit(slot))

FUNCTION Update():
    IF tooltipVisible:
        // Re-validate: member may have left group
        IF hoveredSlot < 0 OR GroupMembers[hoveredSlot] IS NULL:
            HideTooltip()
        RETURN

    IF hoveredSlot < 0: RETURN
    hoverTimer += deltaTime
    IF hoverTimer >= tooltipDelay:
        ShowTooltip(hoveredSlot)

FUNCTION ShowTooltip(slot):
    member = GameData.GroupMembers[slot]
    IF member IS NULL OR member.MyStats IS NULL:
        HideTooltip()
        RETURN

    stats = member.MyStats
    className = stats.CharacterClass?.ClassName ?? "Unknown"

    text = "{name} -- Level {level} {class}\n"
    IF stats.Level < 35:
        text += "XP: {cur:N0} / {need:N0} ({pct:F1}%)\n"
    ELSE:
        text += "Ascension: {cur:N0} / {need:N0} ({pct:F1}%)\n"

    diff = stats.Level - playerLevel
    IF diff > 0: text += "+{diff} levels above you"
    ELSE IF diff < 0: text += "{diff} levels below you"
    ELSE: text += "Same level as you"

    SET tooltipText, POSITION near hovered name, SHOW
```

## 8. Level-Up Handler

```
FUNCTION DoLevelUpPostfix(stats):
    IF stats.Myself.isNPC: RETURN

    IF config.LogLevelCompletion:
        elapsed = Time.time - XPSessionData.LevelStartTime
        LOG_CHAT "Level {level-1} completed in {time} (earned {xp:N0} XP over {kills} kills)"

    XPSessionData.OnLevelUp(stats.Level)
    // Note: OnLevelUp resets LevelTotalXP BEFORE the EarnedXP Postfix
    // records the final gain. This means the triggering kill's XP is
    // attributed to the new level. This is acceptable and documented.
```
