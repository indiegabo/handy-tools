using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.Editor.GraphCore
{
    /// <summary>
    /// Provides shared UI Toolkit helpers for blackboard-variable drag sources.
    /// </summary>
    public static class GraphBlackboardDragSourceUtility
    {
        private static readonly Color IdleColor = new(0f, 0f, 0f, 0f);
        private static readonly Color HoverColor = new(0.23f, 0.23f, 0.23f, 0.72f);

        /// <summary>
        /// Applies the shared hover affordance used by drag-enabled blackboard elements.
        /// </summary>
        /// <param name="dragSource">Element that should look draggable.</param>
        public static void ApplyDragSourceHintStyle(VisualElement dragSource)
        {
            if (dragSource == null)
            {
                return;
            }

            dragSource.style.borderLeftWidth = 2f;
            dragSource.style.borderLeftColor = new Color(0.43f, 0.43f, 0.43f, 0.5f);
            dragSource.style.paddingLeft = Mathf.Max(dragSource.resolvedStyle.paddingLeft, 2f);
        }

        /// <summary>
        /// Registers one pointer-threshold-based drag source for one blackboard entry element.
        /// </summary>
        /// <param name="dragSource">Element that should begin the drag.</param>
        /// <param name="hasActiveDrag">Delegate that reports whether a drag session is active.</param>
        /// <param name="tryBeginDrag">Delegate that starts the drag when the represented entry can be resolved.</param>
        /// <param name="handlePointerDown">Optional callback executed when the primary pointer is pressed.</param>
        /// <param name="handleDragStarted">Optional callback executed after the drag starts successfully.</param>
        public static void RegisterDragSource(
            VisualElement dragSource,
            Func<bool> hasActiveDrag,
            Func<bool> tryBeginDrag,
            Action handlePointerDown = null,
            Action handleDragStarted = null)
        {
            if (dragSource == null || tryBeginDrag == null)
            {
                return;
            }

            Vector2 dragStartPosition = default;
            bool isPointerDown = false;
            Func<bool> resolveHasActiveDrag = hasActiveDrag ?? (() => false);

            dragSource.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                handlePointerDown?.Invoke();
                isPointerDown = true;
                dragStartPosition = evt.mousePosition;
                dragSource.CaptureMouse();
                SetDragSourceVisualState(dragSource, HoverColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseUpEvent>(evt =>
            {
                isPointerDown = false;
                SetDragSourceVisualState(dragSource, IdleColor);

                if (dragSource.HasMouseCapture())
                {
                    dragSource.ReleaseMouse();
                }
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseCaptureOutEvent>(evt =>
            {
                isPointerDown = false;
                SetDragSourceVisualState(dragSource, IdleColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (resolveHasActiveDrag())
                {
                    return;
                }

                SetDragSourceVisualState(dragSource, HoverColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (isPointerDown || resolveHasActiveDrag())
                {
                    return;
                }

                SetDragSourceVisualState(dragSource, IdleColor);
            }, TrickleDown.TrickleDown);

            dragSource.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!isPointerDown || evt.pressedButtons == 0)
                {
                    return;
                }

                if ((evt.mousePosition - dragStartPosition).sqrMagnitude < 16f)
                {
                    return;
                }

                if (!tryBeginDrag())
                {
                    return;
                }

                handleDragStarted?.Invoke();
                isPointerDown = false;
                SetDragSourceVisualState(dragSource, HoverColor);

                if (dragSource.HasMouseCapture())
                {
                    dragSource.ReleaseMouse();
                }

                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
        }

        private static void SetDragSourceVisualState(
            VisualElement dragSource,
            Color backgroundColor)
        {
            if (dragSource == null)
            {
                return;
            }

            dragSource.style.backgroundColor = new StyleColor(backgroundColor);
            dragSource.style.borderLeftColor = backgroundColor.a <= 0.01f
                ? new Color(0.43f, 0.43f, 0.43f, 0.5f)
                : new Color(0.74f, 0.88f, 0.78f, 0.95f);
        }
    }
}