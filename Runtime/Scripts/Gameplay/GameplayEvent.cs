using IndieGabo.HandyTools.HandyBus;

namespace IndieGabo.HandyTools.Gameplay
{
    public struct GameplayStatusChangeEvent : IEvent
    {
        public GameplayService.Status Status { get; set; }
    }
}