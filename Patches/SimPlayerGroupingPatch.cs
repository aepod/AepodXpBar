using HarmonyLib;
using UnityEngine;

namespace AepodXpBar
{
    [HarmonyPatch(typeof(SimPlayerGrouping), "Start")]
    internal static class SimPlayerGroupingPatch
    {
        [HarmonyPostfix]
        static void Postfix(SimPlayerGrouping __instance)
        {
            if (!Plugin.EnableGroupTooltip.Value) return;

            // Lazy-init: attach GroupXPTooltip MonoBehaviour to the grouping object
            var existing = __instance.GetComponent<GroupXPTooltip>();
            if (existing != null) return;

            var tooltip = __instance.gameObject.AddComponent<GroupXPTooltip>();
            tooltip.Init(__instance);
            Plugin.TooltipInstance = tooltip;
        }
    }
}
