namespace IndieGabo.HandyTools
{
    /// <summary>
    /// Compatibility base class for behaviours that previously relied on
    /// Odin serialization.
    /// The class now behaves as a standard HandyBehaviour so the kernel does
    /// not require Sirenix packages.
    /// </summary>
    public abstract class SerializedHandyBehaviour : HandyBehaviour
    {
    }
}