using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.FSMModule;
using IndieGabo.HandyTools.FSMModule.Registering;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    [FilePath("MachineStateVisualizerWindowData.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class MachineStateVisualizerWindowData : ScriptableSingleton<MachineStateVisualizerWindowData>
    {
        [Serializable]
        private sealed class SessionCacheEntry
        {
            [SerializeField]
            private string _machineGlobalId;

            [SerializeField]
            private Session _session;

            public SessionCacheEntry(string machineGlobalId, Session session)
            {
                _machineGlobalId = machineGlobalId;
                _session = session;
            }

            public string MachineGlobalId => _machineGlobalId;
            public Session Session => _session;

            public void SetSession(Session session)
            {
                _session = session;
            }
        }

        [SerializeField]
        private FSMBrain _machine;

        [SerializeField]
        private GameObject _machineObj;

        [SerializeField]
        private string _machineGlobalId;

        [SerializeField]
        private Session _session;

        [SerializeField]
        private List<SessionCacheEntry> _lastSessions = new();

        public FSMBrain Machine => _machine;
        public GameObject MachineObj => _machineObj;
        public string MachineGlobalId => _machineGlobalId;
        public Session Session => _session;


        public void SetMachine(FSMBrain machine)
        {
            SetResolvedMachine(machine);

            if (_machine != null)
            {
                _machineGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_machine).ToString();
            }
            else
            {
                _machineGlobalId = string.Empty;
            }

            Save(true);
        }

        public void SetResolvedMachine(FSMBrain machine)
        {
            _machine = machine;
            if (_machine != null)
            {
                _machineObj = _machine.gameObject;
            }
            else
            {
                _machineObj = null;
            }
        }

        public bool TryResolveMachine(out FSMBrain machine)
        {
            if (_machine != null)
            {
                machine = _machine;
                return true;
            }

            if (TryResolveMachineFromGlobalId(out machine))
            {
                _machine = machine;
                _machineObj = machine.gameObject;
                return true;
            }

            if (_machineObj != null && _machineObj.TryGetComponent(out machine))
            {
                _machine = machine;
                return true;
            }

            machine = null;
            return false;
        }

        private bool TryResolveMachineFromGlobalId(out FSMBrain machine)
        {
            if (string.IsNullOrEmpty(_machineGlobalId) ||
                !GlobalObjectId.TryParse(_machineGlobalId, out GlobalObjectId globalObjectId))
            {
                machine = null;
                return false;
            }

            Object resolvedObject =
                GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);

            if (resolvedObject is FSMBrain resolvedMachine)
            {
                machine = resolvedMachine;
                return true;
            }

            if (resolvedObject is GameObject resolvedGameObject &&
                resolvedGameObject.TryGetComponent(out FSMBrain resolvedFromGameObject))
            {
                machine = resolvedFromGameObject;
                return true;
            }

            machine = null;
            return false;
        }

        public void SetSession(Session session)
        {
            _session = session;
        }

        public bool TryGetLastSession(FSMBrain machine, out Session session)
        {
            if (!TryGetMachineGlobalId(machine, out string machineGlobalId))
            {
                session = null;
                return false;
            }

            for (int index = 0; index < _lastSessions.Count; index++)
            {
                SessionCacheEntry entry = _lastSessions[index];

                if (entry.MachineGlobalId != machineGlobalId)
                {
                    continue;
                }

                session = entry.Session;
                return session != null;
            }

            session = null;
            return false;
        }

        public void StoreLastSession(FSMBrain machine, Session session, bool persist = false)
        {
            if (!TryGetMachineGlobalId(machine, out string machineGlobalId))
            {
                return;
            }

            int entryIndex = FindSessionEntryIndex(machineGlobalId);

            if (entryIndex >= 0)
            {
                _lastSessions[entryIndex].SetSession(session);
            }
            else
            {
                _lastSessions.Add(new SessionCacheEntry(machineGlobalId, session));
            }

            if (_machineGlobalId == machineGlobalId)
            {
                _session = session;
            }

            if (persist)
            {
                Save(true);
            }
        }

        public void ClearLastSessions(bool persist = false)
        {
            _lastSessions.Clear();
            _session = null;

            if (persist)
            {
                Save(true);
            }
        }

        public void Persist()
        {
            Save(true);
        }

        private int FindSessionEntryIndex(string machineGlobalId)
        {
            for (int index = 0; index < _lastSessions.Count; index++)
            {
                if (_lastSessions[index].MachineGlobalId == machineGlobalId)
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool TryGetMachineGlobalId(FSMBrain machine, out string machineGlobalId)
        {
            if (machine == null)
            {
                machineGlobalId = string.Empty;
                return false;
            }

            machineGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(machine).ToString();
            return !string.IsNullOrEmpty(machineGlobalId);
        }

    }
}