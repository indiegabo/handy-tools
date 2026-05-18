#if HANDY_DIALOGUE_SYSTEM_PRESENT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndieGabo.HandyTools.CutscenesModule.Services;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.ThirdParty.DialogueSystem
{
    public sealed class DialogueSystemCutsceneBridge : MonoBehaviour, ICutsceneDialogueBridge
    {
        private const string DialogueManagerTypeName =
            "PixelCrushers.DialogueSystem.DialogueManager";

        private readonly Dictionary<string, CutsceneDialogueResult> _completedResults = new();
        private readonly HashSet<string> _activeHandles = new();

        private bool _hasRegistered;

        public bool IsAvailable => HasDialogueManagerInstance();

        private void Awake()
        {
            TryRegister();
        }

        private void Start()
        {
            TryRegister();
        }

        private void Update()
        {
            if (!_hasRegistered)
            {
                TryRegister();
            }

            if (_activeHandles.Count == 0 || IsConversationActive())
            {
                return;
            }

            List<string> finishedHandles = _activeHandles.ToList();

            for (int i = 0; i < finishedHandles.Count; i++)
            {
                string handleId = finishedHandles[i];
                _completedResults[handleId] = new CutsceneDialogueResult(true, true, string.Empty);
            }

            _activeHandles.Clear();
        }

        public CutsceneDialogueHandle StartConversation(CutsceneDialogueRequest request)
        {
            if (!HasDialogueManagerInstance()
                || string.IsNullOrWhiteSpace(request.ConversationTitle))
            {
                return new CutsceneDialogueHandle(string.Empty);
            }

            string handleId = Guid.NewGuid().ToString("N");
            _completedResults.Remove(handleId);
            _activeHandles.Add(handleId);

            if (!TryStartConversation(
                    request.ConversationTitle,
                    request.Speaker,
                    request.Listener))
            {
                _activeHandles.Remove(handleId);
                return new CutsceneDialogueHandle(string.Empty);
            }

            return new CutsceneDialogueHandle(handleId);
        }

        public bool TryGetResult(CutsceneDialogueHandle handle, out CutsceneDialogueResult result)
        {
            if (_completedResults.TryGetValue(handle.Id, out result))
            {
                return true;
            }

            result = new CutsceneDialogueResult(false, false, string.Empty);
            return false;
        }

        public void CancelConversation(CutsceneDialogueHandle handle)
        {
            if (!handle.IsValid)
            {
                return;
            }

            _activeHandles.Remove(handle.Id);
            _completedResults.Remove(handle.Id);
        }

        private void TryRegister()
        {
            if (_hasRegistered)
            {
                return;
            }

            if (!ServiceLocator.TryGet(out ICutsceneService cutsceneService) || cutsceneService == null)
            {
                return;
            }

            ServiceLocator.Register<ICutsceneDialogueBridge>(this);
            cutsceneService.RegisterDialogueBridge(this);
            _hasRegistered = true;
        }

        private static bool HasDialogueManagerInstance()
        {
            return TryGetDialogueManagerState("hasInstance", out bool value)
                && value;
        }

        private static bool IsConversationActive()
        {
            return TryGetDialogueManagerState("isConversationActive", out bool value)
                && value;
        }

        private static bool TryGetDialogueManagerState(
            string memberName,
            out bool value)
        {
            value = false;

            if (!TryGetDialogueManagerType(out Type dialogueManagerType))
            {
                return false;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            PropertyInfo property = dialogueManagerType.GetProperty(memberName, flags);
            if (property != null && property.PropertyType == typeof(bool))
            {
                object propertyValue = property.GetValue(null);
                if (propertyValue is bool typedPropertyValue)
                {
                    value = typedPropertyValue;
                    return true;
                }
            }

            FieldInfo field = dialogueManagerType.GetField(memberName, flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                object fieldValue = field.GetValue(null);
                if (fieldValue is bool typedFieldValue)
                {
                    value = typedFieldValue;
                    return true;
                }
            }

            return false;
        }

        private static bool TryStartConversation(
            string conversationTitle,
            Transform speaker,
            Transform listener)
        {
            if (!TryGetDialogueManagerType(out Type dialogueManagerType))
            {
                return false;
            }

            MethodInfo startConversationMethod = dialogueManagerType.GetMethod(
                "StartConversation",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Transform), typeof(Transform) },
                null);

            if (startConversationMethod == null)
            {
                return false;
            }

            startConversationMethod.Invoke(
                null,
                new object[] { conversationTitle, speaker, listener });

            return true;
        }

        private static bool TryGetDialogueManagerType(out Type dialogueManagerType)
        {
            dialogueManagerType = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                dialogueManagerType = assembly.GetType(DialogueManagerTypeName, false);
                if (dialogueManagerType != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class DialogueSystemCutsceneBridgeBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!DialogueSystemIntegrationAvailability.IsAvailable())
            {
                return;
            }

            if (ServiceLocator.TryGet(out ICutsceneDialogueBridge existingBridge) && existingBridge != null)
            {
                return;
            }

            GameObject bridgeObject = new("DialogueSystemCutsceneBridge");
            bridgeObject.AddComponent<DialogueSystemCutsceneBridge>();
            UnityEngine.Object.DontDestroyOnLoad(bridgeObject);
        }
    }
}
#endif