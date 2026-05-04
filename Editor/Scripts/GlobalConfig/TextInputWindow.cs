#if UNITY_EDITOR
#nullable enable
using System;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.GlobalConfig
{
    /// <summary>
    /// Minimal helper window to capture text input (Unity lacks a prompt).
    /// </summary>
    internal sealed class TextInputWindow : EditorWindow
    {
        private string _label = "Name:";
        private string _value = string.Empty;
        private Action<string>? _onOk;

        public static string Prompt(string title, string label, string initial)
        {
            string captured = string.Empty;
            var wnd = CreateInstance<TextInputWindow>();
            wnd.titleContent = new GUIContent(title);
            wnd._label = label;
            wnd._value = initial;
            wnd._onOk = v => captured = v;
            wnd.position = new Rect(
                new Vector2(Screen.currentResolution.width / 2f - 150f,
                            Screen.currentResolution.height / 2f - 50f),
                new Vector2(300f, 120f)
            );
            wnd.ShowModalUtility();
            return captured;
        }

        private void OnGUI()
        {
            GUILayout.Label(_label);
            _value = EditorGUILayout.TextField(_value);

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                _onOk?.Invoke(_value?.Trim() ?? string.Empty);
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}
#endif
