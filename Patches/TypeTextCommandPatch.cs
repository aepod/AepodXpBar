using System;
using HarmonyLib;
using UnityEngine;

namespace AepodXpBar
{
    [HarmonyPatch(typeof(TypeText), "CheckCommands")]
    [HarmonyBefore("brumdail.erenshor.qol")]
    internal static class TypeTextCommandPatch
    {
        [HarmonyPrefix]
        static bool Prefix(TypeText __instance)
        {
            if (!Plugin.EnableXPCommand.Value) return true;

            string input = __instance.typed.text.Trim();
            if (!input.StartsWith("/xp", StringComparison.OrdinalIgnoreCase))
                return true; // Not our command, pass to next handler

            // Split on whitespace for robust subcommand parsing (handles double-spaces)
            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1) // "/xp"
            {
                PrintXPStats();
                __instance.typed.text = "";
                return false;
            }

            if (parts.Length == 2 && parts[1].Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                XPSessionData.Reset();
                XPRateCalculator.Reset();
                UpdateSocialLog.LogAdd("[XP] Session tracking reset.", "yellow");
                __instance.typed.text = "";
                return false;
            }

            if (parts.Length == 2 && parts[1].Equals("zone", StringComparison.OrdinalIgnoreCase))
            {
                PrintZoneStats();
                __instance.typed.text = "";
                return false;
            }

            return true; // Unknown /xp subcommand, pass through
        }

        private static void PrintXPStats()
        {
            Stats stats = GameData.PlayerStats;
            bool asc = stats.Level >= 35;
            float now = Time.time;

            string header = asc
                ? $"[XP Stats] Level {stats.Level} {stats.CharacterClass.ClassName} (Ascension)"
                : $"[XP Stats] Level {stats.Level} {stats.CharacterClass.ClassName}";

            UpdateSocialLog.LogAdd(header, "#FFD700");

            int cur = asc ? stats.CurrentAscensionXP : stats.CurrentExperience;
            int need = asc ? stats.AscensionXPtoLevelUp : stats.ExperienceToLevelUp;
            float pct = need > 0 ? (float)cur / need * 100f : 0f;

            string label = asc ? "Ascension XP" : "Current";
            UpdateSocialLog.LogAdd($"  {label}: {cur:N0} / {need:N0} ({pct:F1}%)", "white");

            float rate = XPRateCalculator.GetXPPerHour(now);
            string rateStr = rate > 0 ? $"{rate:N0} XP/hr" : "--- (idle)";
            UpdateSocialLog.LogAdd($"  Rate: {rateStr} (rolling {Plugin.RollingWindowMinutes.Value}m window)", "white");

            float ttl = XPRateCalculator.GetTimeToLevelSeconds(cur, need, now);
            string ttlStr = ttl > 0 ? XPTextFormatter.FormatTimeLong(ttl) : "--- (no data)";
            string ttlLabel = asc ? "Time to Ascension point" : "Time to level";
            UpdateSocialLog.LogAdd($"  {ttlLabel}: {ttlStr}", "white");

            float sessionTime = XPSessionData.SessionStarted
                ? now - XPSessionData.SessionStartTime : 0f;
            float avgXP = XPSessionData.SessionKillCount > 0
                ? (float)XPSessionData.SessionTotalXP / XPSessionData.SessionKillCount : 0f;

            UpdateSocialLog.LogAdd(
                $"  Session: {XPSessionData.SessionTotalXP:N0} XP over {XPSessionData.SessionKillCount} kills " +
                $"(avg {avgXP:F1} XP/kill)", "white");
            UpdateSocialLog.LogAdd(
                $"  Session time: {XPTextFormatter.FormatTimeLong(sessionTime)}", "white");
        }

        private static void PrintZoneStats()
        {
            UpdateSocialLog.LogAdd("[XP] Zone tracking not yet implemented (Phase 2).", "yellow");
        }
    }
}
