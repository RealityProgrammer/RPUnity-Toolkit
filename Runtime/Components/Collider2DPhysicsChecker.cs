using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RealityProgrammer.UnityToolkit.Core.Miscs;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    public class Collider2DPhysicsChecker : MonoBehaviour {
        [field: SerializeField] public Transform Position { get; set; }
        [field: SerializeField] private int _bufferSize = 3;
        public int BufferSize {
            get => _bufferSize;
            set {
                value = Mathf.Clamp(value, 1, 32);
                if (value != _bufferSize) {
                    _bufferSize = value;

                    Array.Resize(ref _raycastHitBuffer, value);
                    Array.Resize(ref _colliderBuffer, value);
                }
            }
        }

        [field: SerializeField] public Vector2 Size { get; set; } = Vector2.one;
        [field: SerializeField] public Vector2 Direction { get; set; } = Vector2.zero;
        [field: SerializeField] public float Distance { get; set; } = 1;
        [field: SerializeField] public float Angle { get; set; }
        [field: SerializeField] public float Radius { get; set; } = 1;

        [field: SerializeField] public LayerMask LayerMask { get; set; }

        [field: SerializeField] public CheckType Type { get; set; }

        [field: SerializeField] public bool CheckEveryFrame { get; set; } = true;

        [field: Tooltip("Requirement tags. Accept all tags if array is empty or null")]
        [field: SerializeField] public string[] TagComparisions { get; set; }

        [field: SerializeField] public UnityEvent StartCollisionCallback { get; set; }
        [field: SerializeField] public UnityEvent EndCollisionCallback { get; set; }

        bool result;

        private int _raycastCount, _colliderCount;
        private RaycastHit2D[] _raycastHitBuffer = new RaycastHit2D[1];
        private Collider2D[] _colliderBuffer = new Collider2D[1];

        private void Awake() {
            _raycastHitBuffer = new RaycastHit2D[_bufferSize];
            _colliderBuffer = new Collider2D[_bufferSize];
        }

        private void Update() {
            if (CheckEveryFrame) {
                ManualCheck();
            }
        }


        public void ManualCheck() {
            bool oldResult = result;
            result = Check();

            if (!oldResult && result) {
                StartCollisionCallback?.Invoke();
            } else if (oldResult && !result) {
                EndCollisionCallback?.Invoke();
            }
        }

        public bool ManualCheckAndReturn() {
            ManualCheck();
            return result;
        }

        public bool Check() {
            switch (Type) {
                case CheckType.BoxCast:
                    _raycastCount = Physics2D.BoxCastNonAlloc(Position.position, Size, Angle, Direction, _raycastHitBuffer, Distance, LayerMask);
                    return _raycastCount > 0 && ValidateHitTags();

                case CheckType.CapsuleCast:
                    _raycastCount = Physics2D.CapsuleCastNonAlloc(Position.position, Size, CapsuleDirection2D.Horizontal, Angle, Direction, _raycastHitBuffer, Distance, LayerMask);
                    return _raycastCount > 0 && ValidateHitTags();

                case CheckType.CircleCast:
                    _raycastCount = Physics2D.CircleCastNonAlloc(Position.position, Radius, Direction, _raycastHitBuffer, Distance, LayerMask);
                    return _raycastCount > 0 && ValidateHitTags();

                case CheckType.LineCast:
                    _raycastCount = Physics2D.LinecastNonAlloc(Position.position, LineCastEnd, _raycastHitBuffer, LayerMask);
                    return _raycastCount > 0 && ValidateHitTags();

                case CheckType.OverlapArea:
                    _colliderCount = Physics2D.OverlapAreaNonAlloc(Position.position, OverlapAreaEnd, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && ValidateHitTags(false);

                case CheckType.OverlapBox:
                    _colliderCount = Physics2D.OverlapBoxNonAlloc(Position.position, Size, Angle, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && ValidateHitTags(false);

                case CheckType.OverlapCapsule:
                    _colliderCount = Physics2D.OverlapCapsuleNonAlloc(Position.position, Size, CapsuleDirection2D.Horizontal, Angle, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && ValidateHitTags(false);

                case CheckType.OverlapCircle:
                    _colliderCount = Physics2D.OverlapCircleNonAlloc(Position.position, Radius, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && ValidateHitTags(false);

                case CheckType.OverlapPoint:
                    _colliderCount = Physics2D.OverlapPointNonAlloc(Position.position, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && ValidateHitTags(false);

                case CheckType.Raycast:
                    _raycastCount = Physics2D.RaycastNonAlloc(Position.position, Direction, _raycastHitBuffer, Distance, LayerMask);
                    return _raycastCount > 0 && ValidateHitTags();

                default:
                    Debug.LogError("Unsupported check type: " + Type);
                    return false;
            }
        }

        bool ValidateHitTags(bool raycast = true) {
            if (TagComparisions == null || TagComparisions.Length == 0) return true;

            if (raycast) {
                for (int t = 0; t < TagComparisions.Length; t++) {
                    for (int r = 0; r < _raycastCount; r++) {
                        if (_raycastHitBuffer[r].collider.CompareTag(TagComparisions[t])) return true;
                    }
                }
            } else {
                for (int t = 0; t < TagComparisions.Length; t++) {
                    for (int c = 0; c < _colliderCount; c++) {
                        if (_colliderBuffer[c].CompareTag(TagComparisions[t])) return true;
                    }
                }
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (Position == null) return;

            Gizmos.color = Check() ? Color.green : Color.red;

            switch (Type) {
                case CheckType.LineCast:
                    Gizmos.DrawLine(Position.position, LineCastEnd);
                    break;

                case CheckType.OverlapArea:
                    Gizmos.DrawWireCube(((Vector2)Position.position + OverlapAreaEnd) / 2, OverlapAreaEnd - (Vector2)Position.position);
                    break;

                case CheckType.OverlapBox:
                    Gizmos.matrix = Matrix4x4.TRS(Position.position, Quaternion.AngleAxis(Angle, Vector3.forward), Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, Size);
                    break;

                case CheckType.OverlapCircle:
                    Gizmos.DrawWireSphere(Position.position, Radius);
                    break;

                case CheckType.OverlapPoint:
                    Gizmos.DrawWireSphere(Position.position, 0.1f);
                    break;

                case CheckType.Raycast:
                    Gizmos.DrawRay(Position.position, Direction * Distance);
                    break;
            }
        }
#endif

        public Vector2 LineCastEnd => (Vector2)Position.position + Direction * Distance;
        public Vector2 OverlapAreaEnd => LineCastEnd;

        public bool Result => result;

        public enum CheckType {
            BoxCast, CapsuleCast, CircleCast, LineCast, OverlapArea, OverlapBox, OverlapCapsule, OverlapCircle, OverlapPoint, Raycast
        }
    }
}