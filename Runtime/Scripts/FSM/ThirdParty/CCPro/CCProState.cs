using System;
using System.Reflection;
using Lightbug.CharacterControllerPro.Core;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.FSMModule.CCPro
{
    /// <summary>
    /// Represents a State controlled by the StateMachine class.
    /// </summary>
    [Serializable]
    public abstract class CCProState : State, ICCProState
    {
        #region Fields

        private ICCProEnvironmentModifierSource _environmentModifierSource;

        #endregion

        #region Getters

        protected FSMBrain CCProBrain => _brain;
        protected FSMBrainCCProDomain CCPro => _brain?.CCPro;
        protected Animator Animator => CCPro?.Animator;
        protected CharacterActor CharacterActor => CCPro?.Actor as CharacterActor;
        protected ICCProEnvironmentModifierSource EnvironmentModifierSource =>
            ResolveEnvironmentModifierSource();
        protected CCProSurfaceModifiers CurrentSurfaceModifiers =>
            EnvironmentModifierSource?.CurrentSurface ?? CCProSurfaceModifiers.Neutral;
        protected CCProVolumeModifiers CurrentVolumeModifiers =>
            EnvironmentModifierSource?.CurrentVolume ?? CCProVolumeModifiers.Neutral;
        protected Vector3 InputMovementReference =>
            CCPro?.InputMovementReference ?? Vector3.zero;
        protected Vector3 MovementReferenceForward =>
            CCPro?.MovementReferenceForward ?? Vector3.forward;
        protected Vector3 MovementReferenceRight =>
            CCPro?.MovementReferenceRight ?? Vector3.right;

        #endregion

        #region Cycle Methods

        public override void Initialize(FSMBrain brain)
        {
            _brain = brain;
            _displayName = GetType().Name;
            ValidateConfiguration();
            SortTransitions();
            Type type = GetType();
            LoadActions(type);

            try
            {
                OnInitAction?.Invoke();
            }
            catch (StateFailureException exception)
            {
                throw exception.WithState(this);
            }
        }

        public virtual void PreCharacterSimulation(float dt)
        {
            InvokeCCProAction(OnPreCharacterSimulationAction, dt);
        }

        public virtual void PostCharacterSimulation(float dt)
        {
            InvokeCCProAction(OnPostCharacterSimulationAction, dt);
        }

        public virtual void PreFixedTick()
        {
            InvokeCCProAction(OnPreFixedTickAction);
        }

        public virtual void PostFixedTick()
        {
            InvokeCCProAction(OnPostFixedTickAction);
        }

        public virtual void TickIK(int layerIndex)
        {
            InvokeCCProAction(OnTickIKAction, layerIndex);
        }

        protected UnityAction<float> OnPreCharacterSimulationAction { get; private set; }
        protected UnityAction<float> OnPostCharacterSimulationAction { get; private set; }
        protected UnityAction OnPreFixedTickAction { get; private set; }
        protected UnityAction OnPostFixedTickAction { get; private set; }
        protected UnityAction<int> OnTickIKAction { get; private set; }

        #endregion

        #region Actions

        /// <summary>
        /// Loads methods as actions to be called during the state's lifecycle.
        /// </summary>
        protected override void LoadActions(Type type)
        {
            base.LoadActions(type);
            OnPreCharacterSimulationAction = GetDelegate<UnityAction<float>>(type, "OnPreCharacterSimulation");
            OnPostCharacterSimulationAction = GetDelegate<UnityAction<float>>(type, "OnPostCharacterSimulation");
            OnPreFixedTickAction = GetDelegate<UnityAction>(type, "OnPreFixedTick");
            OnPostFixedTickAction = GetDelegate<UnityAction>(type, "OnPostFixedTick");
            OnTickIKAction = GetDelegate<UnityAction<int>>(type, "OnTickIK");
        }

        private void ValidateConfiguration()
        {
            if (_brain == null)
            {
                throw new StateFailureException(
                    "Character Controller Pro states require a valid FSMBrain instance.");
            }

            if (CCPro == null || !CCPro.IsEnabled)
            {
                throw new StateFailureException(
                    "Character Controller Pro support is disabled on the FSMBrain.");
            }

            if (CharacterActor == null)
            {
                throw new StateFailureException(
                    "Character Controller Pro support requires a CharacterActor reference.");
            }
        }

        private ICCProEnvironmentModifierSource ResolveEnvironmentModifierSource()
        {
            if (_environmentModifierSource != null || CCProBrain == null)
            {
                return _environmentModifierSource;
            }

            Component[] branchComponents =
                CCProBrain.GetComponentsInChildren<Component>(true);

            for (int index = 0; index < branchComponents.Length; index++)
            {
                if (branchComponents[index] is ICCProEnvironmentModifierSource source)
                {
                    _environmentModifierSource = source;
                    break;
                }
            }

            return _environmentModifierSource;
        }

        private void InvokeCCProAction(UnityAction action)
        {
            try
            {
                action?.Invoke();
            }
            catch (StateFailureException exception)
            {
                throw exception.WithState(this);
            }
        }

        private void InvokeCCProAction(UnityAction<float> action, float value)
        {
            try
            {
                action?.Invoke(value);
            }
            catch (StateFailureException exception)
            {
                throw exception.WithState(this);
            }
        }

        private void InvokeCCProAction(UnityAction<int> action, int value)
        {
            try
            {
                action?.Invoke(value);
            }
            catch (StateFailureException exception)
            {
                throw exception.WithState(this);
            }
        }

        #endregion
    }
}