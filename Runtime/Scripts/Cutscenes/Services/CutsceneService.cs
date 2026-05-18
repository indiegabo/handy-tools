using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Services
{
    public sealed class CutsceneService : MonoBehaviour, ICutsceneService
    {
        private readonly Dictionary<CutsceneDirector, CutsceneRun> _activeRuns = new();

        private ICutsceneDialogueBridge _dialogueBridge;

        public CutsceneRun StartDirector(CutsceneDirector director)
        {
            if (director == null)
            {
                return null;
            }

            if (_activeRuns.TryGetValue(director, out CutsceneRun activeRun))
            {
                switch (director.PlayPolicy)
                {
                    case CutsceneDirectorPlayPolicy.IgnoreIfAlreadyRunning:
                        return activeRun;

                    case CutsceneDirectorPlayPolicy.RestartIfAlreadyRunning:
                    case CutsceneDirectorPlayPolicy.CancelCurrentAndRestart:
                        activeRun.Cancel("Restart requested.");
                        _activeRuns.Remove(director);
                        break;
                }
            }

            CutsceneRun run = new(director, this);
            _activeRuns[director] = run;
            run.Start();
            return run;
        }

        public void StopDirector(CutsceneDirector director, string reason = null)
        {
            if (director == null || !_activeRuns.TryGetValue(director, out CutsceneRun run))
            {
                return;
            }

            run.Cancel(reason ?? "Cancelled by service.");
            _activeRuns.Remove(director);
        }

        public bool IsRunning(CutsceneDirector director)
        {
            return director != null
                && _activeRuns.TryGetValue(director, out CutsceneRun run)
                && run.Status == CutsceneRunStatus.Running;
        }

        public bool TryGetActiveRun(CutsceneDirector director, out CutsceneRun run)
        {
            return _activeRuns.TryGetValue(director, out run);
        }

        public void RegisterDialogueBridge(ICutsceneDialogueBridge bridge)
        {
            _dialogueBridge = bridge;
        }

        public bool TryGetDialogueBridge(out ICutsceneDialogueBridge bridge)
        {
            bridge = _dialogueBridge;
            return bridge != null;
        }

        private void Update()
        {
            List<KeyValuePair<CutsceneDirector, CutsceneRun>> snapshot = _activeRuns.ToList();

            for (int i = 0; i < snapshot.Count; i++)
            {
                KeyValuePair<CutsceneDirector, CutsceneRun> pair = snapshot[i];
                pair.Value.Tick(Time.deltaTime, Time.unscaledDeltaTime);

                if (pair.Value.IsTerminal)
                {
                    _activeRuns.Remove(pair.Key);
                }
            }
        }
    }
}