using System;
using System.Collections.Generic;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieGabo.HandyTools.HandyInputSystemModule.Feedbacks
{
    /// <summary>
    /// A dictionary of <see cref="FeedbackEntry"/> for a <see cref="InputAction"/>.
    /// The key is the <see cref="InputAction"/> ID. 
    /// </summary>
    [Serializable]
    public class FeedbackDictionary : SerializedDictionary<string, FeedbackEntry> { }

}