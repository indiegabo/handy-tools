using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

#if HANDY_DOTWEEN_PRESENT
using DG.Tweening;
#endif

namespace IndieGabo.HandyTools.RenderingModule.Extensions
{
    /// <summary>
    /// Provides light transition helpers, with optional DOTween-backed tweeners
    /// when DOTween is installed in the project.
    /// </summary>
    public static class Light2DExtensions
    {
#if HANDY_DOTWEEN_PRESENT
        /// <summary>
        /// Performs a tween for the light intensity.
        /// </summary>
        /// <param name="light">Target light component.</param>
        /// <param name="targetValue">Target intensity value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>The created DOTween tween.</returns>
        public static Tween DoIntensity(this Light light, float targetValue, float duration)
        {
            return DOTween.To(() => light.intensity, value => light.intensity = value, targetValue, duration);
        }

        /// <summary>
        /// Performs a tween for the light color.
        /// </summary>
        /// <param name="light">Target light component.</param>
        /// <param name="targetColor">Target color value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>The created DOTween tween.</returns>
        public static Tween DoColor(this Light light, Color targetColor, float duration)
        {
            return DOTween.To(() => light.color, value => light.color = value, targetColor, duration);
        }

        /// <summary>
        /// Performs a tween for the Light2D intensity.
        /// </summary>
        /// <param name="light">Target Light2D component.</param>
        /// <param name="targetValue">Target intensity value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>The created DOTween tween.</returns>
        public static Tween DoIntensity(this Light2D light, float targetValue, float duration)
        {
            return DOTween.To(() => light.intensity, value => light.intensity = value, targetValue, duration);
        }

        /// <summary>
        /// Performs a tween for the Light2D color.
        /// </summary>
        /// <param name="light">Target Light2D component.</param>
        /// <param name="targetColor">Target color value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>The created DOTween tween.</returns>
        public static Tween DoColor(this Light2D light, Color targetColor, float duration)
        {
            return DOTween.To(() => light.color, value => light.color = value, targetColor, duration);
        }
#endif

        /// <summary>
        /// Animates the light intensity and returns an awaitable that completes
        /// when the transition ends.
        /// </summary>
        /// <param name="light">Target light component.</param>
        /// <param name="targetValue">Target intensity value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        public static Awaitable DoIntensityAsync(this Light light, float targetValue, float duration)
        {
            return AnimateFloatAsync(
                () => light.intensity,
                value => light.intensity = value,
                targetValue,
                duration
            );
        }

        /// <summary>
        /// Animates the light color and returns an awaitable that completes
        /// when the transition ends.
        /// </summary>
        /// <param name="light">Target light component.</param>
        /// <param name="targetColor">Target color value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        public static Awaitable DoColorAsync(this Light light, Color targetColor, float duration)
        {
            return AnimateColorAsync(
                () => light.color,
                value => light.color = value,
                targetColor,
                duration
            );
        }

        /// <summary>
        /// Animates the Light2D intensity and returns an awaitable that
        /// completes when the transition ends.
        /// </summary>
        /// <param name="light">Target Light2D component.</param>
        /// <param name="targetValue">Target intensity value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        public static Awaitable DoIntensityAsync(this Light2D light, float targetValue, float duration)
        {
            return AnimateFloatAsync(
                () => light.intensity,
                value => light.intensity = value,
                targetValue,
                duration
            );
        }

        /// <summary>
        /// Animates the Light2D color and returns an awaitable that completes
        /// when the transition ends.
        /// </summary>
        /// <param name="light">Target Light2D component.</param>
        /// <param name="targetColor">Target color value.</param>
        /// <param name="duration">Tween duration in seconds.</param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        public static Awaitable DoColorAsync(this Light2D light, Color targetColor, float duration)
        {
            return AnimateColorAsync(
                () => light.color,
                value => light.color = value,
                targetColor,
                duration
            );
        }

        /// <summary>
        /// Animates a float value over time using a quadratic ease-out curve.
        /// </summary>
        /// <param name="getter">Accessor for the current value.</param>
        /// <param name="setter">Mutator for the animated value.</param>
        /// <param name="targetValue">Target value at the end of the animation.</param>
        /// <param name="duration">Transition duration in seconds.</param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        private static async Awaitable AnimateFloatAsync(
            Func<float> getter,
            Action<float> setter,
            float targetValue,
            float duration
        )
        {
            float startingValue = getter();

            if (duration <= 0f)
            {
                setter(targetValue);
                return;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.NextFrameAsync();

                elapsedTime += UnityEngine.Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = EvaluateEaseOutQuad(progress);

                setter(Mathf.Lerp(startingValue, targetValue, easedProgress));
            }

            setter(targetValue);
        }

        /// <summary>
        /// Animates a color value over time using a quadratic ease-out curve.
        /// </summary>
        /// <param name="getter">Accessor for the current value.</param>
        /// <param name="setter">Mutator for the animated value.</param>
        /// <param name="targetValue">Target value at the end of the animation.</param>
        /// <param name="duration">Transition duration in seconds.</param>
        /// <returns>An awaitable that completes when the transition ends.</returns>
        private static async Awaitable AnimateColorAsync(
            Func<Color> getter,
            Action<Color> setter,
            Color targetValue,
            float duration
        )
        {
            Color startingValue = getter();

            if (duration <= 0f)
            {
                setter(targetValue);
                return;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                await Awaitable.NextFrameAsync();

                elapsedTime += UnityEngine.Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float easedProgress = EvaluateEaseOutQuad(progress);

                setter(Color.Lerp(startingValue, targetValue, easedProgress));
            }

            setter(targetValue);
        }

        /// <summary>
        /// Evaluates a quadratic ease-out curve for the provided normalized
        /// progress.
        /// </summary>
        /// <param name="progress">Normalized transition progress.</param>
        /// <returns>The eased normalized progress.</returns>
        private static float EvaluateEaseOutQuad(float progress)
        {
            float inverseProgress = 1f - progress;
            return 1f - (inverseProgress * inverseProgress);
        }
    }
}
