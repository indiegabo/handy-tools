using UnityEngine;
using System;

namespace IndieGabo.HandyTools.Utils
{
    public static class Math
    {

        #region Properties

        /// <summary>
        /// 2 times PI. The total amount of radians to draw a full circle
        /// </summary>
        public static float Tau => 2 * Mathf.PI;

        #endregion

        public static Vector2 RoundDirections(Vector2 directions, float limit = 0.4f)
        {
            if (Mathf.Abs(directions.x) > limit)
                directions.x = MathF.Sign(directions.x);

            if (Mathf.Abs(directions.y) > limit)
                directions.y = Mathf.Sign(directions.y);

            return directions;
        }

        /// <summary>
        /// Converts a float value to a value between 0 and 1   
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static float NormalizeFloat(float value, float maxValue)
        {
            return ConvertNaturalScale(value, maxValue, 0, 1);
        }

        /// <summary>
        /// Converts a float value from one scale to another
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxValue"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static float ConvertNaturalScale(float value, float maxValue, float min, float max)
        {
            float delta = max - min;
            return ((delta * value) + (min * maxValue)) / maxValue;
        }

        /// <summary>
        /// Converts a float value from one scale to another
        /// </summary>
        /// <param name="value"></param>
        /// <param name="currentScaleMin"></param>
        /// <param name="currentScaleMax"></param>
        /// <param name="newScaleMin"></param>
        /// <param name="newScaleMax"></param>
        /// <returns></returns>
        public static float ConvertScale(float value, float currentScaleMin, float currentScaleMax, float newScaleMin, float newScaleMax)
        {
            float currentScaleLength = currentScaleMax - currentScaleMin;
            float newScaleLength = newScaleMax - newScaleMin;

            float offsetValue = value - currentScaleMin;
            float normalizedValue = offsetValue / currentScaleLength;
            float upscaledValue = normalizedValue * newScaleLength;

            return upscaledValue + newScaleMin;
        }

        public static Vector2Int WorldIntoCellPosAddingOrigin(Vector2 worldPos, Vector2 origin)
        {
            int x = Mathf.FloorToInt((worldPos + origin).x);
            int y = Mathf.FloorToInt((worldPos + origin).y);

            return new Vector2Int(x, y);
        }

        public static Vector2Int WorldIntoCellPosDeducingOrigin(Vector2 worldPos, Vector2 origin)
        {
            int x = Mathf.FloorToInt((worldPos - origin).x);
            int y = Mathf.FloorToInt((worldPos - origin).y);

            return new Vector2Int(x, y);
        }

        public static Vector2 WorldIntoNodePos(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos).x);
            int y = Mathf.FloorToInt((worldPos).y);

            return new Vector2(x, y);
        }

        public static Vector2 CenterInWorldPosition(Vector2 worldPos)
        {
            return worldPos + new Vector2(1, 1) * .5f;
        }
    }
}