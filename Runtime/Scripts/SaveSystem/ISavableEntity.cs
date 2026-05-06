using IndieGabo.HandyTools.Utils;

namespace IndieGabo.HandyTools.SaveSystemModule
{
    /// <summary>
    /// Defines the contract required for entities managed by the save system.
    /// </summary>
    public interface ISavableEntity
    {
        /// <summary>
        /// Gets the stable identifier used to persist the entity.
        /// </summary>
        SerializableGuid ID { get; }

        /// <summary>
        /// Gets or sets the boxed persisted payload.
        /// </summary>
        object SavableData { get; set; }

        /// <summary>
        /// Creates the default payload used when no stored data exists.
        /// </summary>
        /// <returns>A default payload instance.</returns>
        object GenerateDefaultData();
    }
}