using System.Text;
using UnityEngine;

namespace AepodXpBar
{
    internal static class XPTextFormatter
    {
        private static readonly StringBuilder _sb = new StringBuilder(128);

        internal static string FormatXPText(Stats stats)
        {
            _sb.Clear();

            bool isAscension = stats.Level >= 35;
            int current, needed;

            if (isAscension)
            {
                current = stats.CurrentAscensionXP;
                needed = stats.AscensionXPtoLevelUp;
                _sb.Append("Ascension: ");
            }
            else
            {
                current = stats.CurrentExperience;
                needed = stats.ExperienceToLevelUp;
            }

            // Core XP numbers
            if (Plugin.FormatNumbers.Value)
            {
                _sb.Append(current.ToString("N0")).Append(" / ").Append(needed.ToString("N0"));
            }
            else if (Plugin.AbbreviateNumbers.Value)
            {
                // IMPORTANT: braces on this branch (expert review fix -- all 3 reviewers flagged)
                AppendAbbreviated(_sb, current);
                _sb.Append(" / ");
                AppendAbbreviated(_sb, needed);
            }
            else
            {
                _sb.Append(current).Append(" / ").Append(needed);
            }

            // Percentage
            if (Plugin.ShowPercentage.Value && needed > 0)
            {
                float pct = (float)current / needed * 100f;
                _sb.Append(" (").Append(pct.ToString($"F{Plugin.PercentageDecimals.Value}")).Append("%)");
            }

            // Rate
            float now = Time.time;
            if (Plugin.ShowRate.Value)
            {
                float rate = XPRateCalculator.GetXPPerHour(now);
                _sb.Append("  |  ");
                if (rate > 0f)
                    _sb.Append(((int)rate).ToString("N0")).Append(" XP/hr");
                else
                    _sb.Append("--- XP/hr");
            }

            // Time to level
            if (Plugin.ShowTimeToLevel.Value)
            {
                _sb.Append("  |  ");
                float ttl = XPRateCalculator.GetTimeToLevelSeconds(current, needed, now);
                if (ttl > 0f)
                    AppendTimeShort(_sb, ttl);
                else
                    _sb.Append("---");
            }

            return _sb.ToString();
        }

        internal static void AppendTimeShort(StringBuilder sb, float seconds)
        {
            if (seconds >= 3600f)
            {
                int hours = (int)(seconds / 3600f);
                int mins = (int)((seconds % 3600f) / 60f);
                sb.Append('~').Append(hours).Append("h ").Append(mins).Append('m');
            }
            else
            {
                int mins = Mathf.Max(1, (int)(seconds / 60f));
                sb.Append('~').Append(mins).Append('m');
            }
        }

        internal static StringBuilder AppendAbbreviated(StringBuilder sb, int value)
        {
            if (value >= 1_000_000)
                sb.AppendFormat("{0:F1}M", value / 1_000_000f);
            else if (value >= 1_000)
                sb.AppendFormat("{0:F1}k", value / 1_000f);
            else
                sb.Append(value);
            return sb;
        }

        /// Format seconds into a long human-readable string for /xp command output.
        internal static string FormatTimeLong(float seconds)
        {
            if (seconds <= 0f) return "0 seconds";

            int hours = (int)(seconds / 3600f);
            int mins = (int)((seconds % 3600f) / 60f);
            int secs = (int)(seconds % 60f);

            if (hours > 0)
                return $"{hours} hour{(hours != 1 ? "s" : "")}, {mins} minute{(mins != 1 ? "s" : "")}";
            if (mins > 0)
                return $"{mins} minute{(mins != 1 ? "s" : "")}, {secs} second{(secs != 1 ? "s" : "")}";
            return $"{secs} second{(secs != 1 ? "s" : "")}";
        }
    }
}
