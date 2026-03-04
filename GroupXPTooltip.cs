using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AepodXpBar
{
    internal class GroupXPTooltip : MonoBehaviour
    {
        // Tooltip UI
        private GameObject _tooltipGO;
        private TextMeshProUGUI _tooltipText;
        private Image _tooltipBg;
        private RectTransform _tooltipRect;
        private RectTransform _canvasRect;
        private Canvas _canvas;

        // State
        private int _hoveredSlot = -1;
        private float _hoverTimer;
        private bool _tooltipVisible;

        // References -- slot containers cover the entire row (name, HP, mana, DPS)
        private GameObject[] _slotContainers;
        private TextMeshProUGUI[] _nameTexts;

        internal void Init(SimPlayerGrouping grouping)
        {
            // Slot containers: hovering anywhere on the member row triggers tooltip
            _slotContainers = new[]
            {
                grouping.ParOne,
                grouping.ParTwo,
                grouping.ParThree,
                grouping.ParFour
            };

            _nameTexts = new[]
            {
                grouping.PlayerOneName,
                grouping.PlayerTwoName,
                grouping.PlayerThreeName,
                grouping.PlayerFourName
            };

            _canvas = grouping.GetComponentInParent<Canvas>();
            if (_canvas == null) return;

            _canvasRect = _canvas.GetComponent<RectTransform>();
            CreateTooltipGameObject();
            AttachEventTriggers();
        }

        private void CreateTooltipGameObject()
        {
            // Parent under canvas root so it renders on top and auto-hides when window closes
            _tooltipGO = new GameObject("AepodXpBar_Tooltip");
            _tooltipGO.transform.SetParent(_canvas.transform, false);
            // Ensure tooltip renders on top of other elements
            _tooltipGO.transform.SetAsLastSibling();

            // Background -- match game's dark panel style
            _tooltipBg = _tooltipGO.AddComponent<Image>();
            _tooltipBg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            _tooltipBg.raycastTarget = false;

            // Outline for subtle border (matches game window edges)
            var outline = _tooltipGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.35f, 0.25f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);

            _tooltipRect = _tooltipGO.GetComponent<RectTransform>();
            _tooltipRect.sizeDelta = new Vector2(240f, 72f);
            _tooltipRect.pivot = new Vector2(0f, 1f); // Anchor top-left

            // Text child
            var textGO = new GameObject("TooltipText");
            textGO.transform.SetParent(_tooltipGO.transform, false);

            _tooltipText = textGO.AddComponent<TextMeshProUGUI>();
            _tooltipText.fontSize = 11f;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAlignmentOptions.TopLeft;
            _tooltipText.enableWordWrapping = true;
            _tooltipText.raycastTarget = false;
            _tooltipText.overflowMode = TextOverflowModes.Overflow;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            _tooltipGO.SetActive(false);
        }

        private void AttachEventTriggers()
        {
            for (int i = 0; i < 4; i++)
            {
                var container = _slotContainers[i];
                if (container == null) continue;

                // Attach to the slot container so hovering anywhere on the row works
                // (name, HP, mana, DPS area all trigger the tooltip)
                var image = container.GetComponent<Image>();
                if (image != null)
                    image.raycastTarget = true;

                var trigger = container.GetComponent<EventTrigger>() ?? container.AddComponent<EventTrigger>();
                int slot = i; // Capture for closure

                AddTriggerEvent(trigger, EventTriggerType.PointerEnter, _ => OnSlotEnter(slot));
                AddTriggerEvent(trigger, EventTriggerType.PointerExit, _ => OnSlotExit(slot));
            }
        }

        private static void AddTriggerEvent(EventTrigger trigger, EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private void OnSlotEnter(int slot)
        {
            _hoveredSlot = slot;
            _hoverTimer = 0f;
        }

        private void OnSlotExit(int slot)
        {
            if (_hoveredSlot == slot)
            {
                _hoveredSlot = -1;
                HideTooltip();
            }
        }

        private void Update()
        {
            if (!Plugin.EnableGroupTooltip.Value)
            {
                if (_tooltipVisible) HideTooltip();
                return;
            }

            // Re-validate visible tooltip: member may have left group
            if (_tooltipVisible)
            {
                if (_hoveredSlot < 0 || _hoveredSlot >= 4
                    || GameData.GroupMembers[_hoveredSlot] == null
                    || GameData.GroupMembers[_hoveredSlot].MyStats == null)
                {
                    HideTooltip();
                    return;
                }

                // Follow mouse while visible
                PositionAtMouse();
                return;
            }

            if (_hoveredSlot < 0) return;

            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= Plugin.TooltipDelay.Value)
            {
                ShowTooltip(_hoveredSlot);
            }
        }

        private void ShowTooltip(int slot)
        {
            if (slot < 0 || slot >= 4) return;

            var member = GameData.GroupMembers[slot];
            if (member == null || member.MyStats == null)
            {
                HideTooltip();
                return;
            }

            var stats = member.MyStats;
            var sb = new StringBuilder();

            // Name + Level + Class (null-safe CharacterClass)
            string className = stats.CharacterClass?.ClassName ?? "Unknown";
            sb.Append("<color=#FFD700>").Append(member.SimName).Append("</color>")
              .Append(" - Lv ").Append(stats.Level).Append(' ').AppendLine(className);

            // XP progress
            if (stats.Level < 35)
            {
                int cur = stats.CurrentExperience;
                int need = stats.ExperienceToLevelUp;
                float pct = need > 0 ? (float)cur / need * 100f : 0f;
                sb.Append("XP: ").Append(cur.ToString("N0")).Append(" / ")
                  .Append(need.ToString("N0")).Append(" (").Append(pct.ToString("F1")).Append("%)");
            }
            else
            {
                int cur = stats.CurrentAscensionXP;
                int need = stats.AscensionXPtoLevelUp;
                float pct = need > 0 ? (float)cur / need * 100f : 0f;
                sb.Append("Ascension: ").Append(cur.ToString("N0")).Append(" / ")
                  .Append(need.ToString("N0")).Append(" (").Append(pct.ToString("F1")).Append("%)");
            }
            sb.AppendLine();

            // Level comparison
            int diff = stats.Level - GameData.PlayerStats.Level;
            if (diff > 0) sb.Append("<color=#00FF00>+").Append(diff).Append(" levels above you</color>");
            else if (diff < 0) sb.Append("<color=#FF6666>").Append(diff).Append(" levels below you</color>");
            else sb.Append("<color=#AAAAAA>Same level as you</color>");

            _tooltipText.text = sb.ToString();

            // Auto-size height based on text
            _tooltipText.ForceMeshUpdate();
            float textHeight = _tooltipText.preferredHeight + 12f; // 12 = padding
            _tooltipRect.sizeDelta = new Vector2(240f, Mathf.Max(60f, textHeight));

            PositionAtMouse();
            _tooltipGO.SetActive(true);
            _tooltipGO.transform.SetAsLastSibling();
            _tooltipVisible = true;
        }

        /// <summary>
        /// Position tooltip near the mouse cursor, following the game's ItemInfoWindow pattern.
        /// Flips direction based on screen position to stay on-screen.
        /// </summary>
        private void PositionAtMouse()
        {
            if (_tooltipRect == null || _canvasRect == null) return;

            Vector2 mousePos = Input.mousePosition;
            Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, mousePos, cam, out Vector2 localPoint);

            // Offset from cursor. Pivot is top-left (0,1), so tooltip extends downward.
            float offsetX = (mousePos.x < Screen.width * 0.5f) ? 16f : -(16f + _tooltipRect.sizeDelta.x);
            // Y: lower half → show above cursor (need +height so bottom sits near cursor)
            //    upper half → show below cursor (top sits just under cursor)
            float offsetY = (mousePos.y < Screen.height * 0.5f)
                ? (8f + _tooltipRect.sizeDelta.y)
                : -8f;

            Vector2 pos = localPoint + new Vector2(offsetX, offsetY);

            // Clamp inside canvas bounds
            Rect canvasR = _canvasRect.rect;
            Vector2 size = _tooltipRect.sizeDelta;
            float pad = 4f;
            pos.x = Mathf.Clamp(pos.x, canvasR.xMin + pad, canvasR.xMax - size.x - pad);
            pos.y = Mathf.Clamp(pos.y, canvasR.yMin + pad, canvasR.yMax - size.y - pad);

            _tooltipRect.anchoredPosition = pos;
        }

        private void HideTooltip()
        {
            if (_tooltipGO != null)
                _tooltipGO.SetActive(false);
            _tooltipVisible = false;
        }

        private void OnDestroy()
        {
            if (_tooltipGO != null)
                Destroy(_tooltipGO);
        }
    }
}
