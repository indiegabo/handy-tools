using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Identifies which Unity loop should drive a runtime update.
    /// </summary>
    public enum UpdateMode
    {
        [InspectorName("Update")]
        Update,
        [InspectorName("Fixed Update")]
        FixedUpdate,
        [InspectorName("Late Update")]
        LateUpdate,
    }
}