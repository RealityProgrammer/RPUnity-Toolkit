using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Miscs {
    public class StateMachine {
        protected Dictionary<Type, State> _stateDictionary;
        public ReadOnlyDictionary<Type, State> StateDictionary => new ReadOnlyDictionary<Type, State>(_stateDictionary);

        protected State entryState;
        public State CurrentState { get; protected set; }

        public MonoBehaviour AssociatedBehaviour { get; protected set; }

        internal static readonly Type StateType = typeof(State);
		
		State lastState = null;
		bool calledStart = false;

        protected StateMachine(MonoBehaviour associated) {
            AssociatedBehaviour = associated;

            _stateDictionary = new Dictionary<Type, State>();
        }

        public virtual void OnEnable() {
            foreach (var pair in _stateDictionary) {
                pair.Value.OnEnable(this);
            }
        }

        public virtual void OnDisable() {
            foreach (var pair in _stateDictionary) {
                pair.Value.OnDisable(this);
            }
        }

        protected void EnsureStartCall() {
            if (!calledStart) {
                calledStart = true;
                CurrentState.Start(this, lastState);
            }
        }

        public virtual void Update() {
            if (CurrentState != null) {
                EnsureStartCall();
				
                CurrentState.Update(this);

                ForceTransitionEvaluate();
            } else {
                Debug.LogError("Trying to Update a state machine with invalid/null state");
            }
        }

        Coroutine durationCoroutine;
        public void ForceTransitionEvaluate() {
            if (durationCoroutine != null) return;

            foreach (var pair in CurrentState.Transitions) {
                if (pair.Value.Condition()) {
                    durationCoroutine = AssociatedBehaviour.StartCoroutine(ApplyCurrentState(pair.Key, pair.Value.TransitionDuration));
                }
            }
        }

        public void ManualStateTransition(Type type, float duration = 0) {
            if (!ValidateStateType(type)) return;

            durationCoroutine = AssociatedBehaviour.StartCoroutine(ApplyCurrentState(type, duration));
        }

        public void ManualStateTransition<T>(float duration = 0) where T : State, new() {
            ManualStateTransition(typeof(T), duration);
        }

        public virtual void FixedUpdate() {
            if (CurrentState != null) {
                EnsureStartCall();

                CurrentState.FixedUpdate(this);
            } else {
                Debug.LogError("Trying to FixedUpdate a state machine with invalid/null state");
            }
        }

        protected virtual IEnumerator ApplyCurrentState(Type type, float duration) {
            if (_stateDictionary.TryGetValue(type, out var state)) {
                if (duration > 0) yield return new WaitForSeconds(duration);

                lastState = CurrentState;
                if (lastState != null) {
                    lastState.Exit(this, state);
                }

                CurrentState = state;
				
				calledStart = false;
                durationCoroutine = null;
            } else {
                throw new ArgumentException("Trying to apply unregistered state.");
            }
        }

        protected IEnumerator ApplyCurrentState<T>(float duration) where T : State {
            yield return ApplyCurrentState(typeof(T), duration);
        }

        /// <summary>
        /// Apply entry state without apply current state.
        /// </summary>
        /// <param name="type">The type represent the state that already been registered</param>
        public void ApplyEntryState(Type type) {
            if (!ValidateStateType(type)) return;

            entryState = _stateDictionary[type];
        }

        /// <summary>
        /// Apply entry state without apply current state.
        /// </summary>
        /// <typeparam name="T">The state type that already been registered</typeparam>
        public void ApplyEntryState<T>() where T : State {
            ApplyEntryState(typeof(T));
        }

        /// <summary>
        /// Apply entry state and current state at the same time.
        /// </summary>
        /// <param name="type">The state type that already been registered</param>
        public void ApplyInitiateState(Type type) {
            if (!ValidateStateType(type)) return;

            ApplyEntryState(type);
            AssociatedBehaviour.StartCoroutine(ApplyCurrentState(type, 0));
        }

        /// <summary>
        /// Apply entry state and current state at the same time.
        /// </summary>
        /// <typeparam name="T">The state they that already registered</typeparam>
        public void ApplyInitiateState<T>() where T : State {
            ApplyInitiateState(typeof(T));
        }

        /// <summary>
        /// Register state type
        /// </summary>
        /// <typeparam name="T">The state type need to be registered, auto assign entry if ID is default. <see cref="State.DefaultState"/></typeparam>
        public bool RegisterState<T>() where T : State, new() {
            return RegisterState(typeof(T));
        }

        /// <summary>
        /// Register state type.
        /// </summary>
        /// <param name="stateType">The state type need to be registered, auto assign entry if ID is default (see <see cref="State.DefaultState"/>). Warning if state is already registered (Editor Only).</param>
        /// <returns>Whether the register operation success.</returns>
        public bool RegisterState(Type stateType) {
            if (!ValidateStateType(stateType)) return false;

            if (!_stateDictionary.ContainsKey(stateType)) {
                State stateInstance = (State)Activator.CreateInstance(stateType);
                _stateDictionary.Add(stateType, stateInstance);

                if (stateInstance.ID == State.DefaultState) {
                    ApplyInitiateState(stateType);
                }

                stateInstance.OnRegistered(this);
                return true;
            }

#if UNITY_EDITOR
            Debug.LogWarning("State of type " + stateType.FullName + " is already exists");
#endif
            return false;
        }

        /// <summary>
        /// Unregister state type.
        /// </summary>
        /// <typeparam name="T">Type of state need to be registered, warning if state doesn't exists (Editor Only).</typeparam>
        public bool UnregisterState<T, TFallback>() where T : State where TFallback : State {
            return UnregisterState(typeof(T), typeof(TFallback));
        }

        /// <summary>
        /// Unregister state type.
        /// </summary>
        /// <param name="stateType">The state type need to be unregistered, warning if state doesn't exists (Editor Only).</param>
        /// <returns>Whether the unregister operation success</returns>
        public bool UnregisterState(Type stateType, Type fallbackState) {
            if (!ValidateStateType(stateType)) return false;

            if (_stateDictionary.ContainsKey(stateType)) {
                _stateDictionary.Remove(stateType);

                if (CurrentState != null && CurrentState.GetType() == stateType) {
                    ManualStateTransition(fallbackState);
                }

                State stateInstance = (State)Activator.CreateInstance(stateType);
                _stateDictionary.Add(stateType, stateInstance);
                stateInstance.OnUnregistered(this);

                return true;
            }

#if UNITY_EDITOR
            Debug.LogWarning("State of type " + stateType.FullName + " is already exists");
#endif
            return false;
        }

        /// <summary>
        /// Apply transition between 2 state type.
        /// </summary>
        /// <typeparam name="TFrom">From state</typeparam>
        /// <typeparam name="TTo">Destination state</typeparam>
        /// <param name="condition">Condition</param>
        /// <param name="transitionDuration">Transition duration once the condition are fulfilled</param>
        public void ApplyTransition<TFrom, TTo>(Func<bool> condition, float transitionDuration = 0) where TFrom : State where TTo : State {
            var tf = typeof(TFrom);
            var tt = typeof(TTo);

            if (_stateDictionary.TryGetValue(tf, out var fromState) && _stateDictionary.ContainsKey(tt)) {
                fromState.Transitions[tt] = Transition.New(transitionDuration, condition);
            } else {
                throw new ArgumentException($"Cannot apply transition state from {tf.FullName} to {tt.FullName} because either Start state or Destination state doesn't exists.");
            }
        }

        protected bool ValidateStateType(Type type) {
            if (type == null) {
                Debug.LogError("The input parameter state type cannot be null.");
            }

            if (!type.IsSubclassOf(StateType)) {
                Debug.LogError("The input parameter state type is not a state.");
                return false;
            }

            if (type.IsAbstract) {
                Debug.LogError("The input parameter state type is abstract, thus cannot be used in the context.");
                return false;
            }

            return true;
        }
    }

    public abstract class State {
        public const int InvalidState = -1;
        public const int DefaultState = 0;

        public Dictionary<Type, Transition> Transitions { get; protected set; }

        public State() {
            Transitions = new Dictionary<Type, Transition>();
        }

        /// <summary>
        /// Represent state ID number.
        /// Use it in a form of <code>public override int ID => (positive value);</code>
        /// Negative state are preserved for internal use only
        /// </summary>
        public abstract int ID { get; }

        public virtual void OnRegistered(StateMachine machine) { }
        public virtual void OnUnregistered(StateMachine machine) { }

        public virtual void OnEnable(StateMachine machine) { }
        public virtual void OnDisable(StateMachine machine) { }

        public virtual void Start(StateMachine machine, State last) { }
        public virtual void Update(StateMachine machine) { }
        public virtual void FixedUpdate(StateMachine machine) { }
        public virtual void Exit(StateMachine machine, State next) { }
    }

    public class Transition {
        public float TransitionDuration { get; private set; }
        public Func<bool> Condition { get; private set; }

        protected Transition() { }

        public static Transition New(float duration, Func<bool> condition) {
            Transition n = new Transition();
            n.TransitionDuration = duration;
            n.Condition = condition;

            return n;
        }
    }
}