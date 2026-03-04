using System.Collections.Generic;
using UnityEngine;

namespace AepodXpBar
{
    internal static class XPRateCalculator
    {
        private static readonly Queue<(float time, int xp)> _window = new Queue<(float, int)>(256);
        private static float _cachedRate;

        // Config-driven, set from Plugin config bindings
        internal static float WindowMinutes = 5f;
        internal static float IdleTimeoutSeconds = 60f;

        internal static void RecordXPGain(int amount, float now)
        {
            _window.Enqueue((now, amount));
            PruneExpired(now);
            RecalculateRate(now);
        }

        internal static float GetXPPerHour(float now)
        {
            if (now - XPSessionData.LastXPGainTime > IdleTimeoutSeconds)
                return 0f;
            return _cachedRate;
        }

        internal static float GetTimeToLevelSeconds(int currentXP, int neededXP, float now)
        {
            float rate = GetXPPerHour(now);
            if (rate <= 0f) return -1f;
            float remaining = neededXP - currentXP;
            return remaining / rate * 3600f;
        }

        /// Called from FixedUpdate rate refresh timer (every 1s) to naturally decay
        /// the rate as old events prune out -- even without new XP events.
        internal static void PruneAndRecalculate(float now)
        {
            PruneExpired(now);
            RecalculateRate(now);
        }

        internal static void Reset()
        {
            _window.Clear();
            _cachedRate = 0f;
        }

        private static void PruneExpired(float now)
        {
            float cutoff = now - (WindowMinutes * 60f);
            while (_window.Count > 0 && _window.Peek().time < cutoff)
                _window.Dequeue();
        }

        private static void RecalculateRate(float now)
        {
            if (_window.Count < 2)
            {
                _cachedRate = 0f;
                return;
            }
            int totalXP = 0;
            foreach (var e in _window)
                totalXP += e.xp;
            float duration = now - _window.Peek().time;
            _cachedRate = duration > 0.5f ? totalXP / duration * 3600f : 0f;
        }
    }
}
