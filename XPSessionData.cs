using UnityEngine;

namespace AepodXpBar
{
    internal static class XPSessionData
    {
        // Session-scoped state (reset on game restart or /xp reset)
        internal static float SessionStartTime;
        internal static int SessionTotalXP;
        internal static int SessionKillCount;
        internal static float LastXPGainTime;
        internal static bool SessionStarted;

        // Level tracking
        internal static int LastKnownLevel;
        internal static float LevelStartTime;
        internal static int LevelTotalXP;

        /// Record an XP gain event.
        internal static void RecordGain(int xpDelta, float now)
        {
            if (!SessionStarted)
            {
                SessionStarted = true;
                SessionStartTime = now;
                LevelStartTime = now;
            }

            SessionTotalXP += xpDelta;
            SessionKillCount++;
            LevelTotalXP += xpDelta;
            LastXPGainTime = now;
        }

        /// Called when player levels up.
        internal static void OnLevelUp(int newLevel)
        {
            LastKnownLevel = newLevel;
            LevelStartTime = Time.time;
            LevelTotalXP = 0;
        }

        /// Reset all session tracking data.
        internal static void Reset()
        {
            SessionStartTime = 0f;
            SessionTotalXP = 0;
            SessionKillCount = 0;
            LastXPGainTime = 0f;
            SessionStarted = false;
            LastKnownLevel = 0;
            LevelStartTime = 0f;
            LevelTotalXP = 0;
        }
    }
}
