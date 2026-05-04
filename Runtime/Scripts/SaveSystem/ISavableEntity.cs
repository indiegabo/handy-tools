using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.SaveSystem
{
    public interface ISavableEntity
    {
        SerializableGuid ID { get; }
        object SavableData { get; set; }
        object GenerateDefaultData();
    }
}