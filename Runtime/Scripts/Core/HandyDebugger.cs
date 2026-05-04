using UnityEngine;

namespace IndieGabo.HandyTools
{
    public abstract class HandyDebugger : HandyBehaviour
    {
        #region Behaviour

        protected virtual void Awake()
        {
#if !UNITY_EDITOR && !HANDY_DEBUG
            Destroy(this);
#endif
        }

        #endregion
    }
}
