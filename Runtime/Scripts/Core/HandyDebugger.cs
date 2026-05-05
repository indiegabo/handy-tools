using UnityEngine;

namespace IndieGabo.HandyTools
{
    /// <summary>
    /// Base behaviour that self-disables in non-debug player builds.
    /// </summary>
    public abstract class HandyDebugger : HandyBehaviour
    {
        #region Behaviour

        /// <summary>
        /// Destroys the component when debug instrumentation is not enabled.
        /// </summary>
        protected virtual void Awake()
        {
#if !UNITY_EDITOR && !HANDY_DEBUG
            Destroy(this);
#endif
        }

        #endregion
    }
}
