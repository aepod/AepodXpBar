using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AepodXpBar
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("Erenshor.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.aepod.erenshor.xpbar";
        public const string NAME = "AepodXpBar";
        public const string VERSION = "1.0.0";

        public static new ManualLogSource Logger;

        // Cached reflection
        public static FieldInfo XPBonusField;

        private Harmony _harmony;

        // --- General ---
        public static ConfigEntry<bool> ModEnabled;

        // --- Display ---
        public static ConfigEntry<bool> FormatNumbers;
        public static ConfigEntry<bool> AbbreviateNumbers;
        public static ConfigEntry<bool> ShowPercentage;
        public static ConfigEntry<int> PercentageDecimals;
        public static ConfigEntry<bool> ShowRate;
        public static ConfigEntry<bool> ShowTimeToLevel;

        // --- Rate ---
        public static ConfigEntry<float> RollingWindowMinutes;
        public static ConfigEntry<float> IdleTimeoutSeconds;

        // --- Commands ---
        public static ConfigEntry<bool> EnableXPCommand;
        public static ConfigEntry<bool> LogLevelCompletion;

        // --- Flash ---
        public static ConfigEntry<bool> EnableFlash;
        public static ConfigEntry<float> FlashDuration;
        public static ConfigEntry<string> FlashColor;

        // --- Group ---
        public static ConfigEntry<bool> EnableGroupTooltip;
        public static ConfigEntry<float> TooltipDelay;

        // Runtime refs (nulled in OnDestroy)
        internal static GroupXPTooltip TooltipInstance;

        void Awake()
        {
            Logger = base.Logger;

            // --- Bind Config ---

            // General
            ModEnabled = Config.Bind("General", "ModEnabled", true,
                "Master toggle for the XP bar enhancement mod.");

            // Display
            FormatNumbers = Config.Bind("Display", "FormatNumbers", true,
                "Display XP numbers with thousands separators (e.g., 12,450).");
            AbbreviateNumbers = Config.Bind("Display", "AbbreviateNumbers", false,
                "Abbreviate large numbers (e.g., 12.5k). Only used when FormatNumbers is false.");
            ShowPercentage = Config.Bind("Display", "ShowPercentage", true,
                "Show percentage completion on the XP bar.");
            PercentageDecimals = Config.Bind("Display", "PercentageDecimals", 1,
                new ConfigDescription("Number of decimal places for the percentage display.",
                    new AcceptableValueRange<int>(0, 2)));
            ShowRate = Config.Bind("Display", "ShowRate", true,
                "Show XP per hour rate on the XP bar.");
            ShowTimeToLevel = Config.Bind("Display", "ShowTimeToLevel", true,
                "Show estimated time to next level on the XP bar.");

            // Rate
            RollingWindowMinutes = Config.Bind("Rate", "RollingWindowMinutes", 5f,
                new ConfigDescription("Duration of the rolling window for XP rate calculation (minutes).",
                    new AcceptableValueRange<float>(1f, 60f)));
            IdleTimeoutSeconds = Config.Bind("Rate", "IdleTimeoutSeconds", 60f,
                new ConfigDescription("Seconds of no XP gain before rate shows as idle.",
                    new AcceptableValueRange<float>(10f, 600f)));

            // Commands
            EnableXPCommand = Config.Bind("Commands", "EnableXPCommand", true,
                "Enable the /xp chat command for session statistics.");
            LogLevelCompletion = Config.Bind("Commands", "LogLevelCompletion", false,
                "Log a chat message when a level is completed with time and XP stats.");

            // Flash
            EnableFlash = Config.Bind("Flash", "EnableFlash", true,
                "Flash the XP text briefly when XP is gained.");
            FlashDuration = Config.Bind("Flash", "FlashDuration", 0.4f,
                new ConfigDescription("Duration of the XP gain flash effect (seconds).",
                    new AcceptableValueRange<float>(0.1f, 2f)));
            FlashColor = Config.Bind("Flash", "FlashColor", "#FFD700",
                "Color of the XP gain flash effect (hex color code).");

            // Group
            EnableGroupTooltip = Config.Bind("Group", "EnableGroupTooltip", true,
                "Show XP tooltip when hovering over group member names.");
            TooltipDelay = Config.Bind("Group", "TooltipDelay", 0.3f,
                new ConfigDescription("Delay before the group XP tooltip appears (seconds).",
                    new AcceptableValueRange<float>(0f, 2f)));

            // --- Cache Reflection ---
            XPBonusField = AccessTools.Field(typeof(Stats), "XPBonus");
            if (XPBonusField == null)
            {
                Logger.LogError("Cannot find Stats.XPBonus field. Mod may not track XP bonuses correctly.");
            }

            // --- Version Compatibility ---
            if (AccessTools.Method(typeof(LiveStatUpdate), "FixedUpdate") == null)
            {
                Logger.LogError("LiveStatUpdate.FixedUpdate not found. Game version incompatible. Aborting patch.");
                return;
            }

            // --- Harmony Patches ---
            _harmony = new Harmony(GUID);
            _harmony.PatchAll();

            // --- SettingChanged Handlers ---
            ModEnabled.SettingChanged += (_, _) =>
            {
                if (ModEnabled.Value)
                {
                    XPRateCalculator.Reset();
                    LiveStatUpdatePatch.InvalidateCache();
                    Logger.LogInfo("AepodXpBar re-enabled. Rate data reset.");
                }
            };

            RollingWindowMinutes.SettingChanged += (_, _) =>
            {
                XPRateCalculator.WindowMinutes = RollingWindowMinutes.Value;
                XPRateCalculator.PruneAndRecalculate(Time.time);
            };

            IdleTimeoutSeconds.SettingChanged += (_, _) =>
            {
                XPRateCalculator.IdleTimeoutSeconds = IdleTimeoutSeconds.Value;
            };

            Logger.LogInfo($"{NAME} v{VERSION} loaded successfully.");
        }

        void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            TooltipInstance = null;
        }
    }
}
