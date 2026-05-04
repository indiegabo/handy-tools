using UnityEngine;

namespace IndieGabo.HandyTools.Utils.Extensions
{
    public static class NumbersExtensions
    {
        /// <summary>
        /// Alters the value defining -1, 0 or 1.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static int Sign(this int number)
        {
            if (number != 0)
                number = number > 0 ? 1 : -1;

            if (number != 0)
                number = number > 0 ? 1 : -1;

            return number;
        }

        /// <summary>
        /// Alters the value defining -1, 0 or 1.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static float Sign(this float number, float min = -1, float max = 1)
        {
            if (number != 0)
                number = number > 0 ? 1 : -1;

            if (number != 0)
                number = number > 0 ? 1 : -1;

            return number;
        }

        public static float PercentageOf(this int part, int whole)
        {
            if (whole == 0) return 0; // Handling division by zero
            return (float)part / whole;
        }

        public static bool Approx(this float f1, float f2) => Mathf.Approximately(f1, f2);
        public static bool IsOdd(this int i) => i % 2 == 1;
        public static bool IsEven(this int i) => i % 2 == 0;

        public static int AtLeast(this int value, int min) => Mathf.Max(value, min);
        public static int AtMost(this int value, int max) => Mathf.Min(value, max);

#if ENABLED_UNITY_MATHEMATICS
        public static half AtLeast(this half value, half max) => MathfExtension.Max(value, max);
        public static half AtMost(this half value, half max) => MathfExtension.Min(value, max);
#endif

        public static float AtLeast(this float value, float min) => Mathf.Max(value, min);
        public static float AtMost(this float value, float max) => Mathf.Min(value, max);

        public static double AtLeast(this double value, double min) => MathfExtension.Max(value, min);
        public static double AtMost(this double value, double min) => MathfExtension.Min(value, min);
    }
}