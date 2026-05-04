using System.Collections;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Provides coroutine helpers that transition Unity time scale.
    /// </summary>
    public static class TimeScalerRoutines
    {
        #region Time Scale

        /// <summary>
        /// Transitions the game into a frozen state.
        /// </summary>
        /// <param name="duration">
        /// Duration in seconds for the transition. Zero applies the target
        /// time scale immediately.
        /// </param>
        /// <returns>A coroutine that completes when the transition ends.</returns>
        public static IEnumerator Freeze(float duration = 0f)
        {
            yield return TransitionIntoTimeScale(0f, duration);
        }

        /// <summary>
        /// Transitions the game back to the default running time scale.
        /// </summary>
        /// <param name="duration">
        /// Duration in seconds for the transition. Zero applies the target
        /// time scale immediately.
        /// </param>
        /// <returns>A coroutine that completes when the transition ends.</returns>
        public static IEnumerator Unfreeze(float duration = 0f)
        {
            yield return TransitionIntoTimeScale(1f, duration);
        }

        /// <summary>
        /// Transitions Unity time scale to the provided value.
        /// </summary>
        /// <param name="targetTimeScale">Target time scale value.</param>
        /// <param name="duration">
        /// Duration in seconds for the transition. Zero applies the target
        /// time scale immediately.
        /// </param>
        /// <returns>A coroutine that completes when the transition ends.</returns>
        public static IEnumerator TransitionIntoTimeScale(
            float targetTimeScale,
            float duration
        )
        {
            if (duration <= 0f)
            {
                UnityEngine.Time.timeScale = targetTimeScale;
                yield break;
            }

            float startingTimeScale = UnityEngine.Time.timeScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += UnityEngine.Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float currentTimeScale = Mathf.Lerp(
                    startingTimeScale,
                    targetTimeScale,
                    progress
                );

                UnityEngine.Time.timeScale = currentTimeScale;
                yield return null;
            }

            UnityEngine.Time.timeScale = targetTimeScale;
        }

        #endregion
    }
}