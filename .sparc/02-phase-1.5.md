# Phase 1.5: Polish -- AepodXpBar

> **Prerequisite**: All Phase 1 tests (T1-T12, T18-T24) pass
> **Tests**: T13, T14-T16, T25 from `completion.md`

---

## Task 1.5.1: Flash Controller

**Create**: `AepodXpBar/FlashController.cs`

### Steps
1. Static class managing flash state:
   - `IsFlashing` (bool)
   - `CurrentColor` (Color)
   - `_flashTimer` (float)
   - `_flashDuration` (float, from config)
   - `_flashColor` (Color, parsed from config hex string)
   - `_normalColor` = `Color.white`
2. `TriggerFlash()`: called from `StatsEarnedXPPatch.Postfix` when delta > 0
   - Set `IsFlashing = true`, `_flashTimer = 0`
   - Set `CurrentColor = _flashColor`
3. `Update(float deltaTime)`: called from `LiveStatUpdatePatch.FixedUpdatePostfix`
   - If not flashing, return
   - `_flashTimer += deltaTime`
   - If `_flashTimer >= _flashDuration`: `IsFlashing = false`, return `_normalColor`
   - Else: `CurrentColor = Color.Lerp(_flashColor, _normalColor, _flashTimer / _flashDuration)`
4. Config integration:
   - `EnableFlash` config gates the feature
   - `FlashDuration` config controls lerp time
   - `FlashColor` config parsed with `ColorUtility.TryParseHtmlString`

### Reference
- Refinement Task 1.5.1
- Architecture Section 3.5 (if exists) or BRD REQ-5

---

## Task 1.5.2: Wire Flash into Patches

**Modify**: `Patches/LiveStatUpdatePatch.cs`, `Patches/StatsEarnedXPPatch.cs`

### Steps
1. In `StatsEarnedXPPatch.Postfix`, after recording gain:
   ```
   if (delta > 0 && Plugin.EnableFlash.Value)
       FlashController.TriggerFlash();
   ```
2. In `LiveStatUpdatePatch.FixedUpdatePostfix`, after setting `instance.XP.text`:
   ```
   if (FlashController.IsFlashing)
   {
       FlashController.Update(Time.deltaTime);
       instance.XP.color = FlashController.CurrentColor;
   }
   else if (instance.XP.color != Color.white)
   {
       instance.XP.color = Color.white;
   }
   ```

### Verify

| Test | Action | Expected |
|------|--------|----------|
| T13 | Kill mob with flash enabled | Text briefly flashes yellow then returns to white |

### Regression
Re-run T1-T12 to confirm no regressions from flash integration.

### Gate
T13 passes, no regressions → proceed to Task 1.5.3.

---

## Task 1.5.3: SimPlayer XP Tooltip (REQ-11)

**Create**: `AepodXpBar/GroupXPTooltip.cs`
**Modify**: `AepodXpBar/Plugin.cs` (lazy init reference)

### Steps
1. Create `GroupXPTooltip` MonoBehaviour:
   - Static reference held in `Plugin` (nulled in `OnDestroy`)
   - Lazy-init: attach via Postfix on `SimPlayerGrouping.Start()` or first `FixedUpdate` when group window exists
2. Find group member name TMP texts:
   - `SimPlayerGrouping.PlayerOneName` through `PlayerFourName`
   - Store in `TextMeshProUGUI[] nameTexts` array
3. Create tooltip GameObject:
   - Parent under group window canvas (auto-hides when window closes)
   - Add `TextMeshProUGUI` component for tooltip text
   - Add `Image` background with semi-transparent dark color
   - Set `CanvasGroup` for fade if desired
4. Attach `EventTrigger` to each name text:
   - Set `nameTexts[i].raycastTarget = true` (NOT BoxCollider2D -- uses GraphicRaycaster)
   - Add `PointerEnter` → `OnSlotEnter(slot)`
   - Add `PointerExit` → `OnSlotExit(slot)`
   - Capture loop variable: `int slot = i`
5. Hover logic in `Update()`:
   - Track `hoveredSlot` (-1 = none), `hoverTimer`, `tooltipVisible`
   - On enter: set `hoveredSlot`, reset `hoverTimer`
   - On exit: set `hoveredSlot = -1`, hide tooltip
   - When `hoverTimer >= TooltipDelay` config: show tooltip
   - While visible: re-validate member (may have left group → hide)
6. `ShowTooltip(int slot)`:
   - Read `GameData.GroupMembers[slot]` → null check
   - Read `member.MyStats` → null check
   - `className = stats.CharacterClass?.ClassName ?? "Unknown"`
   - Build tooltip text:
     - Line 1: `"{name} -- Level {level} {class}"`
     - Line 2: XP progress
       - Level < 35: `"XP: {cur:N0} / {need:N0} ({pct:F1}%)"`
       - Level >= 35: `"Ascension: {cur:N0} / {need:N0} ({pct:F1}%)"`
     - Line 3: Level comparison
       - `"+{diff} levels above you"` / `"{diff} levels below you"` / `"Same level as you"`
   - Position tooltip near hovered name text
7. `HideTooltip()`: disable tooltip GameObject

### Config
- `EnableGroupTooltip` (bool, true) -- gates entire feature
- `TooltipDelay` (float, 0.3) -- seconds before showing

### Reference
- Pseudocode Section 7 (SimPlayer XP Tooltip)
- Architecture Section 5 (GroupXPTooltip)
- Expert review: `raycastTarget = true`, null-safe `CharacterClass`, stale-state validation

### Verify

| Test | Action | Expected |
|------|--------|----------|
| T14 | Hover group member name | XP tooltip appears after 0.3s delay |
| T15 | Hover member at level 35 | Tooltip shows Ascension XP format |
| T16 | Hover empty group slot | No tooltip appears (null safety) |
| T25 | Member departs while hovering | Tooltip hides automatically |

### Regression
Re-run T1-T13 to confirm no regressions.

### Gate
T13-T16, T25 pass, no regressions → Phase 1.5 complete.
