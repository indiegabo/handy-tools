using System.Collections.Generic;

namespace IndieGabo.HandyTools.Utils
{
    public static class Time
    {
        /// <summary>
        /// Formats a string so you can display time passed in
        /// hh:mm:ss format based on the format enum.
        /// </summary>
        /// <param name="timeInSeconds"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string SecondsToClockFormat(int timeInSeconds, ClockFormat format = ClockFormat.Complete)
        {
            int hours = timeInSeconds / 60 / 60;

            if (format == ClockFormat.HoursOnly)
            {
                return hours.ToString("D2");
            }

            int minutes = (timeInSeconds / 60) - (hours * 60);

            if (format == ClockFormat.HoursAndMinutes)
            {
                return hours.ToString("D2") + ":" + minutes.ToString("D2");
            }

            int seconds = timeInSeconds - (hours * 60 * 60) - (minutes * 60);

            return hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
        }

        public enum ClockFormat
        {
            HoursOnly,
            HoursAndMinutes,
            Complete,
        }
    }
}