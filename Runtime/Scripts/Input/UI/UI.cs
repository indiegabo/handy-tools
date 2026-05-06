
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace IndieGabo.HandyTools.HandyInputSystemModule
{
    public static class UI
    {
        public static bool MouseOverUI => IsMouseOverUI();

        /// <summary>
        /// Detects if the mouse is currently over any UI
        /// </summary>
        /// <returns></returns>
        public static bool IsMouseOverUI()
        {
            if (Mouse.current == null) return false;

            PointerEventData pointerEventData = new(EventSystem.current) { position = Mouse.current.position.ReadValue() };
            return IsMouseOverUI(pointerEventData);
        }

        /// <summary>
        /// Detects if the mouse is currently over any UI
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsMouseOverUI(Vector2 pos)
        {
            PointerEventData pointerEventData = new(EventSystem.current) { position = pos };
            return IsMouseOverUI(pointerEventData);
        }

        /// <summary>
        /// Detects if the mouse is currently over any UI
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static bool IsMouseOverUI(Vector2Control pos)
        {
            PointerEventData pointerEventData = new(EventSystem.current) { position = pos.ReadValue() };
            return IsMouseOverUI(pointerEventData);
        }

        /// <summary>
        /// Detects if the mouse is currently over any UI
        /// </summary>
        /// <param name="pointerEventData"></param>
        /// <returns></returns>
        public static bool IsMouseOverUI(PointerEventData pointerEventData)
        {
            List<RaycastResult> raycastResults = new();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
            return raycastResults.Count > 0;
        }
    }
}