using UnityEngine;

namespace IndieGabo.HandyTools.Gameplay
{
    /// <summary>
    /// Persists gameplay-owned local user data outside the SaveSystem module.
    /// </summary>
    public static class GameplayLocalUserData
    {
        private const string _gameplayTimeKey =
            "HandyTools.Gameplay.LocalUserData.TotalGameplayTime";

        /// <summary>
        /// Gets the accumulated gameplay time stored in local user data.
        /// </summary>
        public static float TotalGameplayTime => PlayerPrefs.GetFloat(
            _gameplayTimeKey,
            0f
        );

        /// <summary>
        /// Adds gameplay time to the local user data store.
        /// </summary>
        /// <param name="time">Amount of time to add in seconds.</param>
        public static void RegisterGameplayTime(float time)
        {
            if (time <= 0f)
            {
                return;
            }

            PlayerPrefs.SetFloat(_gameplayTimeKey, TotalGameplayTime + time);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Clears the accumulated gameplay time stored in local user data.
        /// </summary>
        public static void ClearGameplayTime()
        {
            PlayerPrefs.DeleteKey(_gameplayTimeKey);
            PlayerPrefs.Save();
        }
    }
}