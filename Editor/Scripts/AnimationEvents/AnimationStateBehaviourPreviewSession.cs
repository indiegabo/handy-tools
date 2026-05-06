using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.AnimationEventsModule
{
    /// <summary>
    /// Resolves animation-window context for animation-event state behaviour
    /// inspectors.
    /// </summary>
    internal static class AnimationStateBehaviourPreviewSession
    {
        #region Types

        internal readonly struct PreviewContext
        {
            public PreviewContext(
                Animator animator,
                RuntimeAnimatorController runtimeAnimatorController,
                AnimatorController animatorController,
                AnimatorState state,
                Motion motion
            )
            {
                Animator = animator;
                RuntimeAnimatorController = runtimeAnimatorController;
                AnimatorController = animatorController;
                State = state;
                Motion = motion;
            }

            public Animator Animator { get; }

            public RuntimeAnimatorController RuntimeAnimatorController { get; }

            public AnimatorController AnimatorController { get; }

            public AnimatorState State { get; }

            public Motion Motion { get; }
        }

        internal readonly struct AnimationWindowNeedleContext
        {
            public AnimationWindowNeedleContext(
                AnimationWindow animationWindow,
                AnimationClip animationClip,
                int frame,
                float time,
                float normalizedTime
            )
            {
                AnimationWindow = animationWindow;
                AnimationClip = animationClip;
                Frame = frame;
                Time = time;
                NormalizedTime = normalizedTime;
            }

            public AnimationWindow AnimationWindow { get; }

            public AnimationClip AnimationClip { get; }

            public int Frame { get; }

            public float Time { get; }

            public float NormalizedTime { get; }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Tries to resolve the preview context for one behaviour.
        /// </summary>
        /// <param name="behaviour">Behaviour whose owner state should be previewed.</param>
        /// <param name="context">Resolved preview context.</param>
        /// <param name="message">User-facing validation message.</param>
        /// <param name="messageType">Severity associated with the validation message.</param>
        /// <returns>True when the context is valid for preview.</returns>
        public static bool TryCreateContext(
            StateMachineBehaviour behaviour,
            out PreviewContext context,
            out string message,
            out HelpBoxMessageType messageType
        )
        {
            context = default;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = "Preview is disabled while Unity is entering or running Play Mode.";
                messageType = HelpBoxMessageType.Warning;
                return false;
            }

            if (!TryResolvePreviewAnimator(
                    out Animator animator,
                    out _,
                    out message,
                    out messageType
                ))
            {
                return false;
            }

            RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
            if (!TryResolveAnimatorController(
                    runtimeAnimatorController,
                    out AnimatorController animatorController
                ))
            {
                message = "The selected Animator must use an AnimatorController or AnimatorOverrideController.";
                messageType = HelpBoxMessageType.Warning;
                return false;
            }

            if (!TryFindMatchingState(animatorController, behaviour, out AnimatorState state))
            {
                message = "This behaviour is not attached to a state in the selected AnimatorController.";
                messageType = HelpBoxMessageType.Warning;
                return false;
            }

            if (state.motion == null)
            {
                message = "The matching Animator state does not reference a motion.";
                messageType = HelpBoxMessageType.Warning;
                return false;
            }

            context = new PreviewContext(
                animator,
                runtimeAnimatorController,
                animatorController,
                state,
                state.motion
            );

            message = $"Preview target: {animator.name} -> {state.name} ({state.motion.GetType().Name}).";
            messageType = HelpBoxMessageType.Info;
            return true;
        }

        /// <summary>
        /// Tries to resolve the preview Animator from the current inspector
        /// selection or from the active Animation window context.
        /// </summary>
        /// <param name="animator">Resolved preview Animator.</param>
        /// <param name="previewGameObject">Resolved preview GameObject.</param>
        /// <param name="message">User-facing validation message.</param>
        /// <param name="messageType">Severity associated with the validation message.</param>
        /// <returns>True when a preview Animator could be resolved.</returns>
        public static bool TryResolvePreviewAnimator(
            out Animator animator,
            out GameObject previewGameObject,
            out string message,
            out HelpBoxMessageType messageType
        )
        {
            animator = null;

            if (!TryResolvePreviewGameObject(out previewGameObject))
            {
                message = "Select a GameObject with an Animator, or keep the Animation window bound to one, to preview this state.";
                messageType = HelpBoxMessageType.Info;
                return false;
            }

            animator = previewGameObject.GetComponent<Animator>();

            if (animator == null)
            {
                animator = previewGameObject.GetComponentInParent<Animator>();
            }

            if (animator == null)
            {
                message = "The current preview target does not resolve to an Animator component.";
                messageType = HelpBoxMessageType.Warning;
                return false;
            }

            message = string.Empty;
            messageType = HelpBoxMessageType.None;
            return true;
        }


        /// <summary>
        /// Tries to resolve the current Animation window playhead for the clip
        /// that belongs to the inspected state.
        /// </summary>
        /// <param name="behaviour">Behaviour whose owner state should be matched.</param>
        /// <param name="needleContext">Resolved animation-window playhead data.</param>
        /// <param name="message">User-facing validation message.</param>
        /// <param name="messageType">Severity associated with the validation message.</param>
        /// <returns>True when the Animation window playhead can drive this state preview.</returns>
        public static bool TryGetAnimationWindowNeedle(
            StateMachineBehaviour behaviour,
            out AnimationWindowNeedleContext needleContext,
            out string message,
            out HelpBoxMessageType messageType
        )
        {
            needleContext = default;

            if (!TryCreateContext(behaviour, out PreviewContext context, out message, out messageType))
            {
                return false;
            }

            if (!TryResolveAnimationWindowClipContext(
                    context,
                    out AnimationWindow animationWindow,
                    out AnimationClip matchedClip,
                    out message,
                    out messageType
                ))
            {
                return false;
            }

            float frameRate = matchedClip.frameRate > 0f
                ? matchedClip.frameRate
                : 60f;
            int frame = Mathf.Max(0, animationWindow.frame);
            float time = matchedClip.length > 0f
                ? Mathf.Clamp(frame / frameRate, 0f, matchedClip.length)
                : 0f;
            float normalizedTime = matchedClip.length > 0f
                ? Mathf.Clamp01(time / matchedClip.length)
                : 0f;

            needleContext = new AnimationWindowNeedleContext(
                animationWindow,
                matchedClip,
                frame,
                time,
                normalizedTime
            );

            message = $"Animation Window needle: frame {frame} on {matchedClip.name}.";
            messageType = HelpBoxMessageType.Info;
            return true;
        }

        /// <summary>
        /// Tries to move the Animation window playhead to the given normalized
        /// time for the clip used by the inspected state.
        /// </summary>
        /// <param name="behaviour">Behaviour whose owner state should be matched.</param>
        /// <param name="normalizedTime">Normalized time to push into the Animation window.</param>
        /// <param name="message">User-facing validation or result message.</param>
        /// <param name="messageType">Severity associated with the message.</param>
        /// <returns>True when the Animation window playhead was updated.</returns>
        public static bool TrySyncAnimationWindowNeedle(
            StateMachineBehaviour behaviour,
            float normalizedTime,
            out string message,
            out HelpBoxMessageType messageType
        )
        {
            if (!TryCreateContext(behaviour, out PreviewContext context, out message, out messageType))
            {
                return false;
            }

            if (!TryResolveAnimationWindowClipContext(
                    context,
                    out AnimationWindow animationWindow,
                    out AnimationClip matchedClip,
                    out message,
                    out messageType
                ))
            {
                return false;
            }

            float frameRate = matchedClip.frameRate > 0f
                ? matchedClip.frameRate
                : 60f;
            float clampedNormalizedTime = Mathf.Clamp01(normalizedTime);
            float time = matchedClip.length > 0f
                ? clampedNormalizedTime * matchedClip.length
                : 0f;
            int frame = Mathf.Max(0, Mathf.RoundToInt(time * frameRate));
            float snappedTime = frameRate > 0f
                ? frame / frameRate
                : 0f;

            if (matchedClip.length > 0f)
            {
                snappedTime = Mathf.Clamp(snappedTime, 0f, matchedClip.length);
            }

            animationWindow.time = snappedTime;
            animationWindow.frame = frame;
            animationWindow.Repaint();

            message = $"Animation Window needle synced to frame {frame} on {matchedClip.name}.";
            messageType = HelpBoxMessageType.Info;
            return true;
        }

        #endregion

        #region Helpers

        private static bool TryResolveMatchingWindowClip(
            PreviewContext context,
            AnimationClip animationWindowClip,
            out AnimationClip matchedClip
        )
        {
            return TryResolveMatchingWindowClip(
                context.Motion,
                context.RuntimeAnimatorController,
                animationWindowClip,
                out matchedClip
            );
        }

        private static bool TryResolveMatchingWindowClip(
            Motion motion,
            RuntimeAnimatorController runtimeAnimatorController,
            AnimationClip animationWindowClip,
            out AnimationClip matchedClip
        )
        {
            if (motion is AnimationClip clip)
            {
                AnimationClip resolvedClip = ResolveOverrideClip(
                    runtimeAnimatorController,
                    clip
                );

                if (resolvedClip == animationWindowClip)
                {
                    matchedClip = resolvedClip;
                    return true;
                }
            }

            if (motion is BlendTree blendTree)
            {
                ChildMotion[] children = blendTree.children;
                for (int index = 0; index < children.Length; index++)
                {
                    if (TryResolveMatchingWindowClip(
                            children[index].motion,
                            runtimeAnimatorController,
                            animationWindowClip,
                            out matchedClip
                        ))
                    {
                        return true;
                    }
                }
            }

            matchedClip = null;
            return false;
        }

        private static AnimationClip ResolveOverrideClip(
            RuntimeAnimatorController runtimeAnimatorController,
            AnimationClip clip
        )
        {
            if (clip == null)
            {
                return null;
            }

            if (runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                AnimationClip overrideClip = overrideController[clip];
                if (overrideClip != null)
                {
                    return overrideClip;
                }
            }

            return clip;
        }

        private static bool TryResolveAnimationWindowClipContext(
            PreviewContext context,
            out AnimationWindow animationWindow,
            out AnimationClip matchedClip,
            out string message,
            out HelpBoxMessageType messageType
        )
        {
            animationWindow = FindAnimationWindow();
            matchedClip = null;

            if (animationWindow == null)
            {
                message = "Open the Animation window and select the clip used by this state to sync with the playhead needle.";
                messageType = HelpBoxMessageType.Info;
                return false;
            }

            if (animationWindow.animationClip == null)
            {
                message = "The Animation window does not currently have a selected clip.";
                messageType = HelpBoxMessageType.Info;
                return false;
            }

            if (!TryResolveMatchingWindowClip(
                    context,
                    animationWindow.animationClip,
                    out matchedClip
                ))
            {
                message = context.Motion is BlendTree
                    ? "The Animation window clip is not part of this BlendTree, so the needle cannot drive this preview yet."
                    : "The Animation window clip does not match the clip used by this state.";
                messageType = HelpBoxMessageType.Warning;
                return false;
            }

            message = string.Empty;
            messageType = HelpBoxMessageType.None;
            return true;
        }

        #endregion

        #region Discovery

        private static bool TryResolveAnimatorController(
            RuntimeAnimatorController runtimeAnimatorController,
            out AnimatorController animatorController
        )
        {
            animatorController = runtimeAnimatorController as AnimatorController;
            if (animatorController != null)
            {
                return true;
            }

            if (runtimeAnimatorController is AnimatorOverrideController overrideController)
            {
                animatorController = overrideController.runtimeAnimatorController
                    as AnimatorController;
            }

            return animatorController != null;
        }

        private static bool TryFindMatchingState(
            AnimatorController animatorController,
            StateMachineBehaviour behaviour,
            out AnimatorState state
        )
        {
            for (int layerIndex = 0; layerIndex < animatorController.layers.Length; layerIndex++)
            {
                if (TryFindMatchingState(
                        animatorController.layers[layerIndex].stateMachine,
                        behaviour,
                        out state
                    ))
                {
                    return true;
                }
            }

            state = null;
            return false;
        }

        private static bool TryFindMatchingState(
            AnimatorStateMachine stateMachine,
            StateMachineBehaviour behaviour,
            out AnimatorState state
        )
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int stateIndex = 0; stateIndex < states.Length; stateIndex++)
            {
                StateMachineBehaviour[] behaviours = states[stateIndex].state.behaviours;
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    if (ReferenceEquals(behaviours[behaviourIndex], behaviour))
                    {
                        state = states[stateIndex].state;
                        return true;
                    }
                }
            }

            ChildAnimatorStateMachine[] subStateMachines = stateMachine.stateMachines;
            for (int index = 0; index < subStateMachines.Length; index++)
            {
                if (TryFindMatchingState(
                        subStateMachines[index].stateMachine,
                        behaviour,
                        out state
                    ))
                {
                    return true;
                }
            }

            state = null;
            return false;
        }

        private static bool TryResolvePreviewGameObject(out GameObject previewGameObject)
        {
            previewGameObject = Selection.activeGameObject;
            if (previewGameObject != null)
            {
                return true;
            }

            return TryGetAnimationWindowPreviewGameObject(out previewGameObject);
        }

        private static bool TryGetAnimationWindowPreviewGameObject(
            out GameObject previewGameObject
        )
        {
            previewGameObject = null;

            AnimationWindow animationWindow = FindAnimationWindow();
            if (animationWindow == null)
            {
                return false;
            }

            object animationWindowState = GetMemberValue(animationWindow, "state");
            if (animationWindowState == null)
            {
                return false;
            }

            previewGameObject = GetMemberValue<GameObject>(
                animationWindowState,
                "activeRootGameObject"
            );

            if (previewGameObject != null)
            {
                return true;
            }

            previewGameObject = GetMemberValue<GameObject>(
                animationWindowState,
                "activeGameObject"
            );

            return previewGameObject != null;
        }

        private static AnimationWindow FindAnimationWindow()
        {
            if (EditorWindow.focusedWindow is AnimationWindow focusedAnimationWindow)
            {
                return focusedAnimationWindow;
            }

            AnimationWindow[] animationWindows =
                Resources.FindObjectsOfTypeAll<AnimationWindow>();

            return animationWindows != null && animationWindows.Length > 0
                ? animationWindows[0]
                : null;
        }

        private static object GetMemberValue(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            BindingFlags bindingFlags = BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            PropertyInfo propertyInfo = target.GetType().GetProperty(
                memberName,
                bindingFlags
            );

            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(target);
            }

            FieldInfo fieldInfo = target.GetType().GetField(
                memberName,
                bindingFlags
            );

            return fieldInfo != null ? fieldInfo.GetValue(target) : null;
        }

        private static T GetMemberValue<T>(object target, string memberName)
            where T : class
        {
            return GetMemberValue(target, memberName) as T;
        }

        #endregion
    }
}