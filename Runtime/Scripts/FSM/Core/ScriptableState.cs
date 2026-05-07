using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace IndieGabo.HandyTools.FSMModule
{
    public abstract class ScriptableState : ScriptableObject, IState
    {
        #region Inspector

        [SerializeField]
        protected string _name;

        [SerializeField]
        protected string _key;

        #endregion

        #region Fields

        protected FSMBrain _brain;
        protected List<StateTransition> _transitions = new();

        private static readonly Comparison<StateTransition>
            s_transitionPriorityComparison = CompareTransitionsByPriority;

        private static readonly Dictionary<Type, CachedLifecycleMethods>
            s_cachedLifecycleMethods = new();

        #endregion

        #region  Getters

        public string DisplayName => string.IsNullOrEmpty(_name) ? name : _name;
        public FSMBrain Brain => _brain;
        public string Key => _key;

        /// <summary>
        /// Throws a state failure exception so the brain can move through its
        /// error recovery path instead of crashing.
        /// </summary>
        /// <param name="message">Optional message that should be propagated to history.</param>
        /// <param name="innerException">Optional inner exception that caused the failure.</param>
        protected void ThrowStateFailure(
            string message = null,
            Exception innerException = null)
        {
            throw new StateFailureException(message, innerException);
        }

        #endregion

        #region Cycle Methods

        public virtual void Initialize(FSMBrain brain)
        {
            _brain = brain;
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

        public virtual bool CanEnter(IState from) => true;
        public virtual void Enter() { InvokeLifecycleAction(OnEnterAction); }
        public virtual void Exit() { InvokeLifecycleAction(OnExitAction); }
        public virtual void Tick() { InvokeLifecycleAction(OnTickAction); }
        public virtual void FixedTick() { InvokeLifecycleAction(OnFixedTickAction); }
        public virtual void LateTick() { InvokeLifecycleAction(OnLateTickAction); }

        protected UnityAction OnInitAction { get; private set; }
        protected UnityAction OnEnterAction { get; private set; }
        protected UnityAction OnExitAction { get; private set; }
        protected UnityAction OnTickAction { get; private set; }
        protected UnityAction OnLateTickAction { get; private set; }
        protected UnityAction OnFixedTickAction { get; private set; }

        #endregion

        #region Transitions

        /// <summary>
        /// Adds a transition into the available transitions of this state
        /// </summary>
        /// <param name="Condition"> A bool returning callback wich evaluates if the state should become active or not </param>
        /// <param name="targetState"> The state wich should become active based on condition </param>
        /// <param name="priority"> Priority level </param>
        protected virtual void AddTransition(Func<bool> Condition, IState targetState, int priority = 0)
        {
            StateTransition transition = new(Condition, targetState, priority);
            AddTransition(transition);
        }

        /// <summary>
        /// Adds a transition into the available transitions of this state
        /// </summary>
        /// <param name="transition"> The State Trasitions to be add </param>
        protected virtual void AddTransition(StateTransition transition)
        {
            _transitions.Add(transition);
        }

        /// <summary>
        /// Sorts transitions based on priority. Descending
        /// </summary>
        public virtual void SortTransitions()
        {
            if (_transitions.Count <= 1)
            {
                return;
            }

            _transitions.Sort(s_transitionPriorityComparison);
        }

        /// <summary>
        /// Checks if there are valid transitions and sets the output parameter with the target states list.
        /// </summary>
        /// <param name="target">The first valid target state this state wants to transition into.</param>
        /// <returns>True if a valid transition is found, otherwise false.</returns>
        public bool WantsToTransition(out IState target)
        {
            for (int i = 0; i < _transitions.Count; i++)
            {
                StateTransition transition = _transitions[i];

                try
                {
                    if (!transition.ConditionMet())
                    {
                        continue;
                    }
                }
                catch (StateFailureException exception)
                {
                    throw exception.WithState(this);
                }

                target = transition.TargetState;

                try
                {
                    if (target != null && target.CanEnter(this))
                    {
                        return true;
                    }
                }
                catch (StateFailureException exception)
                {
                    throw exception.WithState(target);
                }
            }

            target = null;
            return false;
        }


        /// <summary>
        /// Loads methods as actions to be called during the state's lifecycle.
        /// </summary>
        protected virtual void LoadActions(Type type)
        {
            CachedLifecycleMethods lifecycleMethods =
                GetCachedLifecycleMethods(type);

            OnInitAction = CreateAction(lifecycleMethods.OnInitMethod);
            OnEnterAction = CreateAction(lifecycleMethods.OnEnterMethod);
            OnExitAction = CreateAction(lifecycleMethods.OnExitMethod);
            OnTickAction = CreateAction(lifecycleMethods.OnTickMethod);
            OnLateTickAction = CreateAction(lifecycleMethods.OnLateTickMethod);
            OnFixedTickAction = CreateAction(lifecycleMethods.OnFixedTickMethod);
        }

        private UnityAction CreateAction(MethodInfo method)
        {
            return method == null
                ? null
                : Delegate.CreateDelegate(typeof(UnityAction), this, method) as UnityAction;
        }

        private void InvokeLifecycleAction(UnityAction action)
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

        /// <summary>
        /// Resolves and binds a named instance method to the requested delegate type.
        /// </summary>
        /// <typeparam name="TDelegate">The delegate type to create.</typeparam>
        /// <param name="type">The runtime type that owns the method.</param>
        /// <param name="methodName">The lifecycle method name to resolve.</param>
        /// <returns>The bound delegate instance, or null when the method is not present.</returns>
        protected virtual TDelegate GetDelegate<TDelegate>(
            Type type,
            string methodName)
            where TDelegate : class
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return method == null
                ? null
                : Delegate.CreateDelegate(typeof(TDelegate), this, method) as TDelegate;
        }

        private static CachedLifecycleMethods GetCachedLifecycleMethods(Type type)
        {
            if (s_cachedLifecycleMethods.TryGetValue(type, out CachedLifecycleMethods methods))
            {
                return methods;
            }

            methods = new CachedLifecycleMethods(type);
            s_cachedLifecycleMethods.Add(type, methods);
            return methods;
        }

        private static int CompareTransitionsByPriority(
            StateTransition left,
            StateTransition right)
        {
            return right.Priority.CompareTo(left.Priority);
        }

        private sealed class CachedLifecycleMethods
        {
            public CachedLifecycleMethods(Type type)
            {
                OnInitMethod = GetMethod(type, "OnInit");
                OnEnterMethod = GetMethod(type, "OnEnter");
                OnExitMethod = GetMethod(type, "OnExit");
                OnTickMethod = GetMethod(type, "OnTick");
                OnLateTickMethod = GetMethod(type, "OnLateTick");
                OnFixedTickMethod = GetMethod(type, "OnFixedTick");
            }

            public MethodInfo OnInitMethod { get; }
            public MethodInfo OnEnterMethod { get; }
            public MethodInfo OnExitMethod { get; }
            public MethodInfo OnTickMethod { get; }
            public MethodInfo OnLateTickMethod { get; }
            public MethodInfo OnFixedTickMethod { get; }

            private static MethodInfo GetMethod(Type type, string methodName)
            {
                return type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);
            }
        }

        #endregion
    }
}