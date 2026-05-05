#if ENABLED_UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace IndieGabo.HandyTools.Utils.Extensions
{
    /// <summary>
    /// Provides numeric helpers for types not covered by UnityEngine.Mathf.
    /// </summary>
    public static class MathfExtension
    {
        #region Min

#if ENABLED_UNITY_MATHEMATICS
        /// <summary>
        /// Returns the smaller of two half values.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>The smallest of the two values.</returns>
        public static half Min(half a, half b) {
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Returns the smallest value in the provided half sequence.
        /// </summary>
        /// <param name="values">Values to inspect.</param>
        /// <returns>The smallest value, or zero when none exist.</returns>
        public static half Min(params half[] values) {
            int num = values.Length;
            if (num == 0) {
                return (half) 0;
            }

            half num2 = values[0];
            for (int i = 1; i < num; i++) {
                if (values[i] < num2) {
                    num2 = values[i];
                }
            }

            return num2;
        }
#endif

        /// <summary>
        /// Returns the smaller of two double values.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>The smallest of the two values.</returns>
        public static double Min(double a, double b)
        {
            return (a < b) ? a : b;
        }

        /// <summary>
        /// Returns the smallest value in the provided double sequence.
        /// </summary>
        /// <param name="values">Values to inspect.</param>
        /// <returns>The smallest value, or zero when none exist.</returns>
        public static double Min(params double[] values)
        {
            int num = values.Length;
            if (num == 0)
            {
                return 0f;
            }

            double num2 = values[0];
            for (int i = 1; i < num; i++)
            {
                if (values[i] < num2)
                {
                    num2 = values[i];
                }
            }

            return num2;
        }

        #endregion

        #region Max

#if ENABLED_UNITY_MATHEMATICS
        /// <summary>
        /// Returns the greater of two half values.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>The largest of the two values.</returns>
        public static half Max(half a, half b) {
            return (a > b) ? a : b;
        }

        /// <summary>
        /// Returns the largest value in the provided half sequence.
        /// </summary>
        /// <param name="values">Values to inspect.</param>
        /// <returns>The largest value, or zero when none exist.</returns>
        public static half Max(params half[] values) {
            int num = values.Length;
            if (num == 0) {
                return (half) 0;
            }

            half num2 = values[0];
            for (int i = 1; i < num; i++) {
                if (values[i] > num2) {
                    num2 = values[i];
                }
            }

            return num2;
        }
#endif

        /// <summary>
        /// Returns the greater of two double values.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>The largest of the two values.</returns>
        public static double Max(double a, double b)
        {
            return (a > b) ? a : b;
        }

        /// <summary>
        /// Returns the largest value in the provided double sequence.
        /// </summary>
        /// <param name="values">Values to inspect.</param>
        /// <returns>The largest value, or zero when none exist.</returns>
        public static double Max(params double[] values)
        {
            int num = values.Length;
            if (num == 0)
            {
                return 0f;
            }

            double num2 = values[0];
            for (int i = 1; i < num; i++)
            {
                if (values[i] > num2)
                {
                    num2 = values[i];
                }
            }

            return num2;
        }

        #endregion
    }
}