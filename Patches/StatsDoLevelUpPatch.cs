using HarmonyLib;
using UnityEngine;

namespace AepodXpBar
{
    [HarmonyPatch(typeof(Stats), nameof(Stats.DoLevelUp))]
    internal static class StatsDoLevelUpPatch
    {
        [HarmonyPostfix]
        static void Postfix(Stats __instance)
        {
            if (__instance.Myself.isNPC) return;

            // Log level completion (optional, config-gated)
            if (Plugin.LogLevelCompletion.Value)
            {
                float elapsed = Time.time - XPSessionData.LevelStartTime;
                string timeStr = XPTextFormatter.FormatTimeLong(elapsed);
                UpdateSocialLog.LogAdd(
                    $"Level {__instance.Level - 1} completed in {timeStr} " +
                    $"(earned {XPSessionData.LevelTotalXP:N0} XP over {XPSessionData.SessionKillCount} kills)",
                    "yellow"
                );
            }

            XPSessionData.OnLevelUp(__instance.Level);
        }
    }
}
