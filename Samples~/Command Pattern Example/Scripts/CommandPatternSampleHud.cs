using System.Text;
using UnityEngine;

namespace IndieGabo.HandyTools.CommandPatternModule.Samples
{
    /// <summary>
    /// Renders the sample controls and ordered request list through IMGUI so
    /// the sample scene does not depend on authored UI assets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommandPatternSampleHud : MonoBehaviour
    {
        [SerializeField]
        private CommandPatternSampleController _controller;

        [SerializeField]
        private Rect _panelRect = new(16f, 16f, 360f, 520f);

        private Vector2 _requestScrollPosition;

        private void OnGUI()
        {
            if (_controller == null)
            {
                return;
            }

            GUILayout.BeginArea(_panelRect, GUI.skin.window);
            DrawHeader();
            DrawMovementButtons();
            DrawSchedulingButtons();
            DrawHistoryButtons();
            DrawRuntimeSummary();
            DrawRequestList();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.Label("Command Pattern Example", GUI.skin.box);

            if (_controller.GridActor != null)
            {
                GUILayout.Label(
                    $"Grid Position: {_controller.GridActor.GridPosition}");
            }
        }

        private void DrawMovementButtons()
        {
            GUILayout.Label("Immediate Moves");

            GUILayout.BeginHorizontal();
            DrawButton("Up", _controller.MoveUp);
            DrawButton("Down", _controller.MoveDown);
            DrawButton("Left", _controller.MoveLeft);
            DrawButton("Right", _controller.MoveRight);
            GUILayout.EndHorizontal();
        }

        private void DrawSchedulingButtons()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Scheduled Moves");

            GUILayout.BeginHorizontal();
            DrawButton("Next Frame Up", _controller.ScheduleMoveUpNextFrame);
            DrawButton("Scaled Right", _controller.ScheduleMoveRightScaled);
            DrawButton("Unscaled Left", _controller.ScheduleMoveLeftUnscaled);
            GUILayout.EndHorizontal();
        }

        private void DrawHistoryButtons()
        {
            GUILayout.Space(4f);
            GUILayout.Label("History");

            GUILayout.BeginHorizontal();
            DrawButton("Undo", _controller.UndoLast);
            DrawButton("Redo", _controller.RedoLast);
            DrawButton("Reset", _controller.ResetSample);
            GUILayout.EndHorizontal();
        }

        private void DrawRuntimeSummary()
        {
            CommandJournalSnapshot snapshot = _controller.GetSnapshot();
            StringBuilder builder = new();
            builder.Append("Pending: ").Append(snapshot.Pending.Count);
            builder.Append(" | Running: ").Append(snapshot.Running.Count);
            builder.Append(" | Completed: ").Append(snapshot.Completed.Count);
            builder.Append(" | Failed: ").Append(snapshot.Failed.Count);
            builder.Append(" | Undone: ").Append(snapshot.Undone.Count);
            builder.Append(" | Redone: ").Append(snapshot.Redone.Count);

            GUILayout.Space(4f);
            GUILayout.Label(builder.ToString(), GUI.skin.box);
        }

        private void DrawRequestList()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Requested Commands");

            _requestScrollPosition = GUILayout.BeginScrollView(
                _requestScrollPosition,
                GUILayout.Height(240f));

            if (_controller.RequestLog == null
                || _controller.RequestLog.Entries.Count == 0)
            {
                GUILayout.Label("No requests submitted yet.");
                GUILayout.EndScrollView();
                return;
            }

            for (int index = 0; index < _controller.RequestLog.Entries.Count; index++)
            {
                GUILayout.Label(
                    $"{index + 1}. {_controller.RequestLog.Entries[index]}",
                    GUI.skin.box);
            }

            GUILayout.EndScrollView();
        }

        private static void DrawButton(string label, System.Action action)
        {
            if (GUILayout.Button(label, GUILayout.Height(28f)))
            {
                action?.Invoke();
            }
        }
    }
}