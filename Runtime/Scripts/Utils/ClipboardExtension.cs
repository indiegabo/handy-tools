using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    public static class ClipboardExtension
    {
        /// <summary>
        /// Puts the string into the Clipboard.
        /// </summary>
        public static void CopyToClipboard(this string str)
        {
            GUIUtility.systemCopyBuffer = str;
            Debug.Log($"[Clipboard] \"{str}\" copied to clipboard");
        }
    }
}
