using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    public interface ICCProState : IState
    {
        void PreCharacterSimulation(float dt);
        void PostCharacterSimulation(float dt);

        void PreFixedTick();
        void PostFixedTick();
        void TickIK(int layerIndex);
    }
}