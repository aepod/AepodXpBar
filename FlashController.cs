using UnityEngine;

namespace AepodXpBar
{
    internal static class FlashController
    {
        internal static bool IsFlashing;
        internal static Color CurrentColor = Color.white;

        private static float _flashTimer;
        private static float _flashDuration;
        private static Color _flashColor;
        private static readonly Color _normalColor = Color.white;

        /// <summary>Trigger a flash effect. Call from StatsEarnedXPPatch when XP is gained.</summary>
        internal static void TriggerFlash()
        {
            _flashDuration = Plugin.FlashDuration.Value;

            if (!ColorUtility.TryParseHtmlString(Plugin.FlashColor.Value, out _flashColor))
                _flashColor = new Color(1f, 0.843f, 0f); // #FFD700 gold fallback

            IsFlashing = true;
            _flashTimer = 0f;
            CurrentColor = _flashColor;
        }

        /// <summary>Tick the flash lerp. Call from LiveStatUpdatePatch every FixedUpdate.</summary>
        internal static void Update(float deltaTime)
        {
            if (!IsFlashing) return;

            _flashTimer += deltaTime;
            if (_flashTimer >= _flashDuration)
            {
                IsFlashing = false;
                CurrentColor = _normalColor;
            }
            else
            {
                CurrentColor = Color.Lerp(_flashColor, _normalColor, _flashTimer / _flashDuration);
            }
        }
    }
}
