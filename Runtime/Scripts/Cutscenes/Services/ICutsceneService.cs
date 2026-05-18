using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem;

namespace IndieGabo.HandyTools.CutscenesModule.Services
{
    public interface ICutsceneService
    {
        CutsceneRun StartDirector(CutsceneDirector director);

        void StopDirector(CutsceneDirector director, string reason = null);

        bool IsRunning(CutsceneDirector director);

        bool TryGetActiveRun(CutsceneDirector director, out CutsceneRun run);

        void RegisterDialogueBridge(ICutsceneDialogueBridge bridge);

        bool TryGetDialogueBridge(out ICutsceneDialogueBridge bridge);
    }
}