using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Miscs {
    public class StateMachine {
        protected Dictionary<Type, State> _stateDictionary;
        protected State entryState;
        public State CurrentState { get; protected set; }

        protected StateMachine() {
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

        public void ForceTransitionEvaluate() {
            foreach (var pair in CurrentState.Transitions) {
                if (pair.Value()) {
                    ApplyCurrentState(pair.Key);
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

        protected void ApplyCurrentState(Type type) {
            if (_stateDictionary.TryGetValue(type, out var state)) {
                var last = CurrentState;
                if (last != null) {
                    last.Exit(this, state);
                }

                CurrentState = state;
                state.Start(this, last);
            } else {
                throw new ArgumentException("Trying to apply unregistered state.");
            }
        }

        protected void ApplyCurrentState<T>() where T : State {
            ApplyCurrentState(typeof(T));
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
            ApplyCurrentState<T>();
        }

        public void RegisterState<T>() where T : State, new() {
            var type = typeof(T);

            if (!_stateDictionary.ContainsKey(type)) {
                _stateDictionary.Add(type, new T());
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

        public void ApplyTransition<TFrom, TTo>(Func<bool> condition) where TFrom : State where TTo : State {
            var tf = typeof(TFrom);
            var tt = typeof(TTo);

            if (_stateDictionary.TryGetValue(tf, out var fromState) && _stateDictionary.ContainsKey(tt)) {
                fromState.Transitions[tt] = condition;
            } else {
                throw new ArgumentException($"Cannot apply transition state from {tf.FullName} to {tt.FullName} because either Start state or Destination state doesn't exists.");
            }
        }
    }

    public abstract class State {
        public const int InvalidState = -1;
        public const int DefaultState = 0;

        public Dictionary<Type, Func<bool>> Transitions { get; protected set; }

        public State() {
            Transitions = new Dictionary<Type, Func<bool>>();
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
}