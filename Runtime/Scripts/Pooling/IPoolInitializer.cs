namespace IndieGabo.HandyTools.Pooling
{
    /// <summary>
    /// Defines lifecycle hooks for components that create and dispose pools.
    /// </summary>
    public interface IPoolInitializer
    {
        /// <summary>
        /// Creates or activates the owned pool resources.
        /// </summary>
        void InitializePool();

        /// <summary>
        /// Releases or deactivates the owned pool resources.
        /// </summary>
        void DismissPool();
    }
}
