using System.Collections.Generic;
using IndieGabo.HandyTools.FSMModule;
using IndieGabo.HandyTools.FSMModule.Registering;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    /// <summary>
    /// Captures the latest playable-session history for FSM brains while the editor is in play mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class MachineStateHistoryTracker
    {
        private const int HistorySize = 256;
        private const double ScanIntervalInSeconds = 0.5d;

        private sealed class SessionRecorder
        {
            private readonly FSMBrain _machine;

            private Session _session;
            private float _sessionStartTime;
            private bool _isRecording;

            /// <summary>
            /// Creates a recorder bound to a single FSM brain.
            /// </summary>
            /// <param name="machine">The machine whose transitions should be recorded.</param>
            public SessionRecorder(FSMBrain machine)
            {
                _machine = machine;
                _machine.StatusChanged.AddListener(OnStatusChanged);
                _machine.StateChanged.AddListener(OnStateChanged);

                if (_machine.IsOn)
                {
                    StartSession();
                }
            }

            /// <summary>
            /// Gets the tracked machine.
            /// </summary>
            public FSMBrain Machine => _machine;

            /// <summary>
            /// Stops recording and detaches all listeners.
            /// </summary>
            /// <param name="persistSession">Whether the recorded session should remain stored.</param>
            public void Dispose(bool persistSession)
            {
                if (_machine != null)
                {
                    _machine.StatusChanged.RemoveListener(OnStatusChanged);
                    _machine.StateChanged.RemoveListener(OnStateChanged);
                }

                FinalizeSession(persistSession);
            }

            /// <summary>
            /// Starts a fresh session for the current machine run.
            /// </summary>
            private void StartSession()
            {
                if (_machine == null || !_machine.ShouldCaptureHistory)
                {
                    return;
                }

                _session = new Session(_machine, HistorySize);
                _sessionStartTime = Time.realtimeSinceStartup;
                _isRecording = true;

                if (_machine.CurrentState != null)
                {
                    _session.Register(_machine.CurrentState, _machine.LastTransitionReport);
                }

                MachineStateVisualizerWindowData.instance.StoreLastSession(
                    _machine,
                    _session);
            }

            /// <summary>
            /// Finalizes the current session and optionally keeps it cached.
            /// </summary>
            /// <param name="persistSession">Whether the finalized session should be cached.</param>
            private void FinalizeSession(bool persistSession)
            {
                if (!_isRecording || _session == null)
                {
                    _isRecording = false;
                    _session = null;
                    return;
                }

                _session.Close(Time.realtimeSinceStartup - _sessionStartTime);

                if (persistSession)
                {
                    MachineStateVisualizerWindowData.instance.StoreLastSession(
                        _machine,
                        _session);
                }

                _isRecording = false;
                _session = null;
            }

            /// <summary>
            /// Reacts to runtime machine status changes.
            /// </summary>
            /// <param name="status">The updated machine status.</param>
            private void OnStatusChanged(MachineStatus status)
            {
                switch (status)
                {
                    case MachineStatus.On:
                        StartSession();
                        break;
                    case MachineStatus.Off:
                        FinalizeSession(true);
                        break;
                }
            }

            /// <summary>
            /// Captures each runtime transition into the current session.
            /// </summary>
            /// <param name="state">The new active state.</param>
            /// <param name="previous">The previous active state.</param>
            private void OnStateChanged(IState state, IState previous)
            {
                if (!_isRecording || _session == null || state == null)
                {
                    return;
                }

                _session.Register(state, _machine.LastTransitionReport);
                MachineStateVisualizerWindowData.instance.StoreLastSession(
                    _machine,
                    _session);
            }
        }

        private static readonly Dictionary<EntityId, SessionRecorder> _recorders = new();

        private static double _nextScanTime;

        /// <summary>
        /// Initializes the play-mode tracking hooks.
        /// </summary>
        static MachineStateHistoryTracker()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// Responds to editor play-mode lifecycle changes.
        /// </summary>
        /// <param name="mode">The new editor play-mode state.</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            switch (mode)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    BeginTracking();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    EndTracking();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    MachineStateVisualizerWindowData.instance.Persist();
                    break;
            }
        }

        /// <summary>
        /// Periodically discovers newly loaded FSM brains while play mode is running.
        /// </summary>
        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;

            if (currentTime < _nextScanTime)
            {
                return;
            }

            _nextScanTime = currentTime + ScanIntervalInSeconds;
            SyncTrackedMachines();
        }

        /// <summary>
        /// Clears stale history and starts tracking the current play-mode machines.
        /// </summary>
        private static void BeginTracking()
        {
            MachineStateVisualizerWindowData.instance.ClearLastSessions(true);
            _nextScanTime = 0d;
            SyncTrackedMachines();
        }

        /// <summary>
        /// Finalizes all active recorders and persists the last captured sessions.
        /// </summary>
        private static void EndTracking()
        {
            foreach (SessionRecorder recorder in _recorders.Values)
            {
                recorder.Dispose(true);
            }

            _recorders.Clear();
            MachineStateVisualizerWindowData.instance.Persist();
        }

        /// <summary>
        /// Synchronizes the tracked recorder set with the machines currently loaded in play mode.
        /// </summary>
        private static void SyncTrackedMachines()
        {
            FSMBrain[] machines = Object.FindObjectsByType<FSMBrain>(
                FindObjectsInactive.Include);

            HashSet<EntityId> validMachineIds = new();

            for (int index = 0; index < machines.Length; index++)
            {
                FSMBrain machine = machines[index];

                if (machine == null || !machine.ShouldCaptureHistory)
                {
                    continue;
                }

                EntityId machineId = machine.GetEntityId();
                validMachineIds.Add(machineId);

                if (_recorders.ContainsKey(machineId))
                {
                    continue;
                }

                _recorders.Add(machineId, new SessionRecorder(machine));
            }

            List<EntityId> idsToRemove = new();

            foreach (KeyValuePair<EntityId, SessionRecorder> pair in _recorders)
            {
                if (pair.Value.Machine != null && validMachineIds.Contains(pair.Key))
                {
                    continue;
                }

                pair.Value.Dispose(false);
                idsToRemove.Add(pair.Key);
            }

            for (int index = 0; index < idsToRemove.Count; index++)
            {
                _recorders.Remove(idsToRemove[index]);
            }
        }
    }
}