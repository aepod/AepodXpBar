using HarmonyLib;
using UnityEngine;

namespace AepodXpBar
{
    [HarmonyPatch(typeof(Stats), nameof(Stats.EarnedXP))]
    internal static class StatsEarnedXPPatch
    {
        // Uses Harmony's __state parameter for Prefix-to-Postfix data passing.
        // This is the idiomatic pattern -- inherently reentrant-safe and self-documenting.
        // Thread safety: all calls are on Unity's main thread. EarnedXP does not recurse.

        [HarmonyPrefix]
        static void Prefix(Stats __instance, out (int xp, int level) __state)
        {
            if (__instance.Myself.isNPC)
            {
                __state = default;
                return;
            }

            __state = (
                __instance.Level < 35 ? __instance.CurrentExperience : __instance.CurrentAscensionXP,
                __instance.Level
            );
        }

        [HarmonyPostfix]
        static void Postfix(Stats __instance, int _incomingXP, (int xp, int level) __state)
        {
            if (__instance.Myself.isNPC) return;

            // XP Lock check -- if locked, EarnedXP returns early and no XP was gained
            if (GameData.XPLock == 1) return;

            int levelAfter = __instance.Level;
            int xpAfter = __instance.Level < 35
                ? __instance.CurrentExperience
                : __instance.CurrentAscensionXP;

            int delta;
            if (levelAfter > __state.level)
            {
                // Level-up occurred within EarnedXP. DoLevelUp() set CurrentExperience = 0.
                // Overflow XP is lost in vanilla. Use CalculateActualGain to compute
                // _incomingXP + bonus, which is the total XP that was added before the reset.
                delta = CalculateActualGain(__instance, _incomingXP);
            }
            else if (xpAfter < __state.xp && __instance.Level >= 35)
            {
                // Ascension point gained: CurrentAscensionXP reset to 0
                delta = CalculateActualGain(__instance, _incomingXP);
            }
            else
            {
                delta = xpAfter - __state.xp;
            }

            if (delta > 0)
            {
                float now = Time.time;
                XPSessionData.RecordGain(delta, now);
                XPRateCalculator.RecordXPGain(delta, now);

                if (Plugin.EnableFlash.Value)
                    FlashController.TriggerFlash();
            }
        }

        private static int CalculateActualGain(Stats stats, int incomingXP)
        {
            // XPBonus is private -- use cached FieldInfo from Plugin.Awake()
            float bonus = 0f;
            if (Plugin.XPBonusField != null)
                bonus = (float)Plugin.XPBonusField.GetValue(stats);
            int bonusXP = Mathf.RoundToInt(incomingXP * bonus);
            return incomingXP + bonusXP;
        }
    }
}
