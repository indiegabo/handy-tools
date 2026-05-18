using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.LoggerModule;
using Sirenix.OdinInspector;
using UnityEngine;

namespace IndieGabo.HandyTools.CutscenesModule.Nodes.Actions
{
    [System.Serializable]
    [CutsceneNodeMenu("Actions/Log", "Log")]
    public sealed class CutsceneLogNode : CutsceneNodeBase
    {
        [SerializeField]
        [CutsceneValueSourceType(typeof(string))]
        private CutsceneValueSource _messageSource =
            CutsceneValueSource.CreateDirect("Cutscene log");

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _warningSource =
            CutsceneValueSource.CreateDirect(false);

        [SerializeField]
        [CutsceneValueSourceType(typeof(bool))]
        private CutsceneValueSource _errorSource =
            CutsceneValueSource.CreateDirect(false);

        public void Configure(string message, bool warning = false, bool error = false)
        {
            EnsureValueSourcesConfigured();
            _messageSource.SetDirectValue(message ?? string.Empty);
            _warningSource.SetDirectValue(warning);
            _errorSource.SetDirectValue(error);
        }

        public override string GetSummary()
        {
            EnsureValueSourcesConfigured();
            return _messageSource.GetSummary();
        }

        public override void OnEnter(CutsceneExecutionContext context)
        {
            EnsureValueSourcesConfigured();

            if (!_messageSource.TryGetValue(context, out string message))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Log node requires one valid message source."));
                return;
            }

            if (!_warningSource.TryGetValue(context, out bool warning)
                || !_errorSource.TryGetValue(context, out bool error))
            {
                context.TryComplete(CutsceneNodeResult.Failure(
                    "Log node requires valid severity sources."));
                return;
            }

            if (error)
            {
                HandyLogger.Error(nameof(CutsceneLogNode), message);
            }
            else if (warning)
            {
                HandyLogger.Warning(nameof(CutsceneLogNode), message);
            }
            else
            {
                HandyLogger.Message(nameof(CutsceneLogNode), message);
            }

            context.TryComplete(CutsceneNodeResult.Success());
        }

        private void EnsureValueSourcesConfigured()
        {
            _messageSource ??= CutsceneValueSource.CreateDirect("Cutscene log");
            _warningSource ??= CutsceneValueSource.CreateDirect(false);
            _errorSource ??= CutsceneValueSource.CreateDirect(false);

            _messageSource.SetExpectedValueType(typeof(string));
            _warningSource.SetExpectedValueType(typeof(bool));
            _errorSource.SetExpectedValueType(typeof(bool));
        }
    }
}