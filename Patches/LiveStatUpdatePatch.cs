using System;
using HarmonyLib;
using UnityEngine;

namespace AepodXpBar
{
    [HarmonyPatch(typeof(LiveStatUpdate), nameof(LiveStatUpdate.FixedUpdate))]
    internal static class LiveStatUpdatePatch
    {
        // Caching: avoid regenerating the string 50x/sec when nothing changed.
        // Regenerate when: (a) XP changed, (b) level changed, (c) 1-second rate refresh timer elapsed.
        private static int _lastXP = int.MinValue;
        private static int _lastLevel = -1;
        private static string _cached;
        private static float _nextRateRefresh;

        [HarmonyPostfix]
        static void Postfix(LiveStatUpdate __instance)
        {
            if (!Plugin.ModEnabled.Value) return;

            // Null guards: prevent 50Hz log spam during loading screens or scene transitions
            if (__instance.PlayerStats == null || __instance.XP == null) return;

            try
            {
                Stats stats = __instance.PlayerStats;
                int curXP = stats.Level < 35 ? stats.CurrentExperience : stats.CurrentAscensionXP;
                float now = Time.time;

                bool xpChanged = curXP != _lastXP || stats.Level != _lastLevel;
                bool rateStale = now >= _nextRateRefresh;

                if (xpChanged || rateStale || _cached == null)
                {
                    // Trigger rate decay even when no new XP events
                    if (rateStale)
                    {
                        XPRateCalculator.PruneAndRecalculate(now);
                        _nextRateRefresh = now + 1f;
                    }

                    _cached = XPTextFormatter.FormatXPText(stats);
                    _lastXP = curXP;
                    _lastLevel = stats.Level;

                    __instance.XP.text = _cached;
                }
                // When cached: skip XP.text setter entirely (TMP already has this text)

                // Flash effect: apply color lerp every frame while flashing
                if (FlashController.IsFlashing)
                {
                    FlashController.Update(Time.deltaTime);
                    __instance.XP.color = FlashController.CurrentColor;
                }
                else if (__instance.XP.color != Color.white)
                {
                    __instance.XP.color = Color.white;
                }
            }
            catch (Exception ex)
            {
                // Rate-limit error logging to avoid 50Hz log spam on persistent errors
                if (Time.frameCount % 500 == 0)
                    Plugin.Logger.LogWarning($"XP format error: {ex.Message}");
            }
        }

        /// <summary>Called when ModEnabled transitions false->true to invalidate cache.</summary>
        internal static void InvalidateCache()
        {
            _lastXP = int.MinValue;
            _cached = null;
        }
    }
}
