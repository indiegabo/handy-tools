using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule
{
    public interface IState
    {
        string Key { get; }
        string DisplayName { get; }
        FSMBrain Brain { get; }

        bool WantsToTransition(out IState target);
        bool CanEnter(IState from);
        void Initialize(FSMBrain machine);
        void Enter();
        void Exit();
        void Tick();
        void FixedTick();
        void LateTick();
    }
}