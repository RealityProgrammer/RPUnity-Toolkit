using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Miscs {
    public class StateMachine {
        protected Dictionary<Type, State> _stateDictionary;
        protected State entryState;
        public State CurrentState { get; protected set; }

        private MonoBehaviour AssociatedBehaviour;

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

        public virtual void Update() {
            if (CurrentState != null) {
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

        public virtual void FixedUpdate() {
            if (CurrentState != null) {
                CurrentState.FixedUpdate(this);
            } else {
                Debug.LogError("Trying to FixedUpdate a state machine with invalid/null state");
            }
        }

        protected virtual IEnumerator ApplyCurrentState(Type type, float duration) {
            if (_stateDictionary.TryGetValue(type, out var state)) {
                if (duration > 0) yield return new WaitForSeconds(duration);

                var last = CurrentState;
                if (last != null) {
                    last.Exit(this, state);
                }

                CurrentState = state;
                state.Start(this, last);

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
        /// <typeparam name="T">The state type that already registered</typeparam>
        public void ApplyEntryState<T>() where T : State {
            entryState = _stateDictionary[typeof(T)];
        }

        /// <summary>
        /// Apply entry state and current state at the same time.
        /// </summary>
        /// <typeparam name="T">The state they that already registered</typeparam>
        public void ApplyInitiateState<T>() where T : State {
            ApplyEntryState<T>();
            AssociatedBehaviour.StartCoroutine(ApplyCurrentState<T>(0));
        }

        public void RegisterState<T>() where T : State, new() {
            var type = typeof(T);

            if (!_stateDictionary.ContainsKey(type)) {
                T stateInstance = new T();
                _stateDictionary.Add(type, stateInstance);

                if (stateInstance.ID == State.DefaultState) {
                    ApplyInitiateState<T>();
                }
            } else {
                throw new ArgumentException("State of type " + type.FullName + " is already exists");
            }
        }

        public void RegisterState<T>(T state) where T : State {
            if (state == null) return;

            var type = state.GetType();

            if (!_stateDictionary.ContainsKey(type)) {
                _stateDictionary.Add(type, state);
            } else {
                throw new ArgumentException("State of type " + type.FullName + " is already exists");
            }
        }

        /// <summary>
        /// Apply transition between 2 state type
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