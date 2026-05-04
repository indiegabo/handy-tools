using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Provides awaitable helpers that transition Unity time scale.
    /// </summary>
    public static class TimeScalerAsync
    {
        #region Time Scale

        /// <summary>
        /// Transitions the game into a frozen state.
        /// </summary>
        /// <param name="duration">
        /// Duration in seconds for the transition. Zero applies the target
        /// time scale immediately.
        /// </param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        public static Awaitable Freeze(float duration = 0f)
        {
            return TransitionIntoTimeScale(0f, duration);
        }

        /// <summary>
        /// Transitions the game back to the default running time scale.
        /// </summary>
        /// <param name="duration">
        /// Duration in seconds for the transition. Zero applies the target
        /// time scale immediately.
        /// </param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        public static Awaitable Unfreeze(float duration = 0f)
        {
            return TransitionIntoTimeScale(1f, duration);
        }

        /// <summary>
        /// Transitions Unity time scale to the provided value.
        /// </summary>
        /// <param name="targetTimeScale">Target time scale value.</param>
        /// <param name="duration">
        /// Duration in seconds for the transition. Zero applies the target
        /// time scale immediately.
        /// </param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        public static async Awaitable TransitionIntoTimeScale(
            float targetTimeScale,
            float duration
        )
        {
            if (duration <= 0f)
            {
                UnityEngine.Time.timeScale = targetTimeScale;
                return;
            }

            float startingTimeScale = UnityEngine.Time.timeScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.NextFrameAsync();

                elapsedTime += UnityEngine.Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = EvaluateEaseInQuad(progress);

                UnityEngine.Time.timeScale = Mathf.Lerp(
                    startingTimeScale,
                    targetTimeScale,
                    easedProgress
                );
            }

            UnityEngine.Time.timeScale = targetTimeScale;
        }

        private static float EvaluateEaseInQuad(float progress)
        {
            return progress * progress;
        }

        #endregion
    }
}