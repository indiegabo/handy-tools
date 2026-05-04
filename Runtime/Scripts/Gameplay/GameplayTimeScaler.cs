using Cysharp.Threading.Tasks;
using UnityEngine;

namespace IndieGabo.HandyTools.Gameplay
{
    /// <summary>
    /// Provides local gameplay time-scale transitions without relying on a
    /// separate module bootstrap order.
    /// </summary>
    internal static class GameplayTimeScaler
    {
        /// <summary>
        /// Transitions time scale to zero.
        /// </summary>
        /// <param name="duration">Transition duration in seconds.</param>
        /// <returns>An asynchronous task that completes when the transition ends.</returns>
        public static UniTask Freeze(float duration = 0f)
        {
            return TransitionIntoTimeScale(0f, duration);
        }

        /// <summary>
        /// Transitions time scale back to one.
        /// </summary>
        /// <param name="duration">Transition duration in seconds.</param>
        /// <returns>An asynchronous task that completes when the transition ends.</returns>
        public static UniTask Unfreeze(float duration = 0f)
        {
            return TransitionIntoTimeScale(1f, duration);
        }

        /// <summary>
        /// Interpolates the global time scale over an optional duration.
        /// </summary>
        /// <param name="targetTimeScale">Target time scale value.</param>
        /// <param name="duration">Transition duration in seconds.</param>
        /// <returns>An asynchronous task that completes when the transition ends.</returns>
        private static async UniTask TransitionIntoTimeScale(
            float targetTimeScale,
            float duration
        )
        {
            if (duration <= 0f)
            {
                Time.timeScale = targetTimeScale;
                return;
            }

            float startingTimeScale = Time.timeScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = progress * progress;
                Time.timeScale = Mathf.Lerp(
                    startingTimeScale,
                    targetTimeScale,
                    easedProgress
                );

                await UniTask.NextFrame();
            }

            Time.timeScale = targetTimeScale;
        }
    }
}