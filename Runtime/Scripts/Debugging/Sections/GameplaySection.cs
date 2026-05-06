using IndieGabo.HandyTools.GameplayModule;
using IndieGabo.HandyTools.HandyBusModule;
using IndieGabo.HandyTools.HandyServiceLocatorModule;
using UnityEngine;
using UnityEngine.UIElements;

namespace IndieGabo.HandyTools.DebuggingModule
{
    [DebugPanelSection]
    /// <summary>
    /// Debug panel section that exposes live gameplay lifecycle telemetry.
    /// </summary>
    public sealed class GameplaySection : DebugPanelSection
    {
        private const string _sectionTitle = "Gameplay";

        private Label _statusValueLabel;
        private Label _targetStatusValueLabel;
        private Label _sessionValueLabel;
        private Label _ownerValueLabel;
        private Label _lastTransitionValueLabel;
        private Label _lastOwnerValueLabel;

        private GameplayService _gameplayService;
        private EventSubscription<GameplayStatusChangeEvent> _eventSubscription;
        private GameplayStatusChangeEvent _lastEvent;
        private bool _hasLastEvent;

        /// <summary>
        /// Gets the display order of the section inside the panel.
        /// </summary>
        public override int OrderInPanel => 10;

        /// <summary>
        /// Initializes the section and subscribes to gameplay lifecycle events.
        /// </summary>
        /// <param name="panel">Owning debug panel host.</param>
        public override void Initialize(DebugPanel panel)
        {
            base.Initialize(panel);
            ServiceLocator.TryGet(out _gameplayService);
            _eventSubscription = HandyBus<GameplayStatusChangeEvent>.Subscribe(
                OnGameplayEvent
            );
        }

        /// <summary>
        /// Releases the gameplay event subscription when the section is
        /// destroyed.
        /// </summary>
        private void OnDestroy()
        {
            _eventSubscription.Dispose();
        }

        /// <summary>
        /// Builds the UI Toolkit element used by the section.
        /// </summary>
        /// <returns>The root visual element of the section.</returns>
        public override VisualElement BuildSectionElement()
        {
            Foldout foldout = new()
            {
                text = _sectionTitle,
                value = false,
            };

            foldout.AddToClassList("section-container");
            foldout.style.flexGrow = 1f;
            foldout.style.paddingTop = 5f;
            foldout.style.paddingRight = 5f;
            foldout.style.paddingBottom = 5f;
            foldout.style.paddingLeft = 5f;

            _statusValueLabel = AddRow(foldout, "Current Status");
            _targetStatusValueLabel = AddRow(foldout, "Transition Target");
            _sessionValueLabel = AddRow(foldout, "Session");
            _ownerValueLabel = AddRow(foldout, "Current Interruption Owner");
            _lastTransitionValueLabel = AddRow(foldout, "Last Transition");
            _lastOwnerValueLabel = AddRow(foldout, "Last Transition Owner");

            RefreshTelemetry();

            return foldout;
        }

        /// <summary>
        /// Refreshes the displayed telemetry while the panel is visible.
        /// </summary>
        private void Update()
        {
            if (!Panel.IsOpen)
            {
                return;
            }

            RefreshTelemetry();
        }

        /// <summary>
        /// Stores the latest gameplay event so the section can expose a stable
        /// transition snapshot to the user.
        /// </summary>
        /// <param name="@event">Received gameplay lifecycle event.</param>
        private void OnGameplayEvent(GameplayStatusChangeEvent @event)
        {
            _lastEvent = @event;
            _hasLastEvent = true;
            RefreshTelemetry();
        }

        /// <summary>
        /// Creates one label/value row inside the foldout.
        /// </summary>
        /// <param name="parent">Parent element that receives the row.</param>
        /// <param name="label">Static row label text.</param>
        /// <returns>The dynamic value label for the row.</returns>
        private static Label AddRow(VisualElement parent, string label)
        {
            VisualElement row = new()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    marginBottom = 3f,
                }
            };

            Label titleLabel = new(label)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.87f, 0.87f, 0.87f),
                    marginRight = 8f,
                }
            };

            Label valueLabel = new("-")
            {
                style =
                {
                    color = new Color(0.93f, 0.93f, 0.93f),
                    unityTextAlign = TextAnchor.MiddleRight,
                    flexGrow = 1f,
                    whiteSpace = WhiteSpace.Normal,
                }
            };

            row.Add(titleLabel);
            row.Add(valueLabel);
            parent.Add(row);

            return valueLabel;
        }

        /// <summary>
        /// Refreshes the live gameplay telemetry labels.
        /// </summary>
        private void RefreshTelemetry()
        {
            if (_statusValueLabel == null)
            {
                return;
            }

            if (_gameplayService == null)
            {
                ServiceLocator.TryGet(out _gameplayService);
            }

            if (_gameplayService == null)
            {
                SetUnavailableTelemetry();
                return;
            }

            GameplaySessionContext sessionContext =
                _gameplayService.CurrentSessionContext;

            _statusValueLabel.text = _gameplayService.CurrentStatus.ToString();
            _targetStatusValueLabel.text = _gameplayService.IsTransitioning
                ? _gameplayService.TransitionTargetStatus.ToString()
                : "-";
            _sessionValueLabel.text = sessionContext.IsValid
                ? $"#{sessionContext.SessionSequence} / T{sessionContext.TransitionIndex} ({sessionContext.SessionId})"
                : "none";
            _ownerValueLabel.text = _gameplayService.HasInterruptionOwner
                ? _gameplayService.CurrentInterruptionOwnerDescription
                : "none";

            if (_hasLastEvent)
            {
                _lastTransitionValueLabel.text =
                    $"{_lastEvent.Origin} ({_lastEvent.PreviousStatus} -> {_lastEvent.Status})";
                _lastOwnerValueLabel.text = _lastEvent.HasInterruptionOwner
                    ? _lastEvent.InterruptionOwnerDescription
                    : "none";
                return;
            }

            _lastTransitionValueLabel.text = "none";
            _lastOwnerValueLabel.text = "none";
        }

        /// <summary>
        /// Shows fallback text when the gameplay service is not available.
        /// </summary>
        private void SetUnavailableTelemetry()
        {
            _statusValueLabel.text = "service unavailable";
            _targetStatusValueLabel.text = "service unavailable";
            _sessionValueLabel.text = "service unavailable";
            _ownerValueLabel.text = "service unavailable";
            _lastTransitionValueLabel.text = _hasLastEvent
                ? $"{_lastEvent.Origin} ({_lastEvent.PreviousStatus} -> {_lastEvent.Status})"
                : "none";
            _lastOwnerValueLabel.text = _hasLastEvent
                ? _lastEvent.InterruptionOwnerDescription
                : "none";
        }
    }
}