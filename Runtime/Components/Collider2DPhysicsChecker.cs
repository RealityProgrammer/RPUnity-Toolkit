using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RealityProgrammer.UnityToolkit.Core.Miscs;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    public sealed class Collider2DPhysicsChecker : MonoBehaviour {
        [field: SerializeField] public Transform Position { get; set; }
        [field: SerializeField] private int _bufferSize = 3;
        public int BufferSize {
            get => _bufferSize;
            set {
#if UNITY_EDITOR
                value = Mathf.Clamp(value, 1, 32);

                if (Application.isPlaying) {
                    if (value != _bufferSize) {
                        _bufferSize = value;

                        Array.Resize(ref _raycastHitBuffer, value);
                        Array.Resize(ref _colliderBuffer, value);
                    }
                } else {
                    Array.Resize(ref _raycastHitBuffer, value);
                    Array.Resize(ref _colliderBuffer, value);
                }
#else
                value = Mathf.Clamp(value, 1, 32);

                if (value != bufferSize) {
                    _bufferSize = value;

                    Array.Resize(ref _raycastHitBuffer, value);
                    Array.Resize(ref _colliderBuffer, value);
                }
#endif
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
        [field: SerializeField] public List<string> TagComparisions { get; set; }

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

        private IEnumerable<RaycastHit2D> filteredRaycastResults;
        public bool Check() {
            switch (Type) {
                case CheckType.BoxCast:
                    _raycastCount = Physics2D.BoxCastNonAlloc(Position.position, Size, Angle, Direction, _raycastHitBuffer, Distance, LayerMask);

                    return ValidateRaycastResults();

                case CheckType.CapsuleCast:
                    _raycastCount = Physics2D.CapsuleCastNonAlloc(Position.position, Size, CapsuleDirection2D.Horizontal, Angle, Direction, _raycastHitBuffer, Distance, LayerMask);
                    return ValidateRaycastResults();

                case CheckType.CircleCast:
                    _raycastCount = Physics2D.CircleCastNonAlloc(Position.position, Radius, Direction, _raycastHitBuffer, Distance, LayerMask);
                    return ValidateRaycastResults();

                case CheckType.LineCast:
                    _raycastCount = Physics2D.LinecastNonAlloc(Position.position, LineCastEnd, _raycastHitBuffer, LayerMask);
                    return ValidateRaycastResults();

                case CheckType.OverlapArea:
                    _colliderCount = Physics2D.OverlapAreaNonAlloc(Position.position, OverlapAreaEnd, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && FilterColliderTag().Any();

                case CheckType.OverlapBox:
                    _colliderCount = Physics2D.OverlapBoxNonAlloc(Position.position, Size, Angle, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && FilterColliderTag().Any();

                case CheckType.OverlapCapsule:
                    _colliderCount = Physics2D.OverlapCapsuleNonAlloc(Position.position, Size, CapsuleDirection2D.Horizontal, Angle, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && FilterColliderTag().Any();

                case CheckType.OverlapCircle:
                    _colliderCount = Physics2D.OverlapCircleNonAlloc(Position.position, Radius, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && FilterColliderTag().Any();

                case CheckType.OverlapPoint:
                    _colliderCount = Physics2D.OverlapPointNonAlloc(Position.position, _colliderBuffer, LayerMask);
                    return _colliderCount > 0 && FilterColliderTag().Any();

                case CheckType.Raycast:
                    _raycastHitBuffer = Physics2D.RaycastAll(Position.position, Direction, Distance);
                    _raycastCount = _raycastHitBuffer.Length;

                    return ValidateRaycastResults();

                default:
                    Debug.LogError("Unsupported check type: " + Type);
                    return false;
            }
        }

        #region Raycast Results
        bool ValidateRaycastResults() {
            if (_raycastCount == 0) return false;

            filteredRaycastResults = FilterRaycastResults();
            return filteredRaycastResults.Any();
        }
        public RaycastHit2D[] FilteredRaycastResults {
            get {
                if (_raycastCount == 0) return null;

                return filteredRaycastResults.ToArray();
            }
        }
        bool ValidatePlatformEffector(RaycastHit2D raycast) {
            if (raycast.collider.TryGetComponent<PlatformEffector2D>(out var effector)) {
                var effectorUp = Quaternion.AngleAxis(effector.rotationalOffset, Vector3.forward) * Vector3.up;
                var differenceAngle = Vector2.SignedAngle(effectorUp, raycast.normal);

                return Mathf.Abs(differenceAngle) <= effector.surfaceArc / 2;
            } else {
                return true;
            }
        }
        IEnumerable<RaycastHit2D> FilterRaycastResults() {
            if (TagComparisions == null || TagComparisions.Count == 0) {
                for (int i = 0; i < _raycastCount; i++)
                    if (ValidatePlatformEffector(_raycastHitBuffer[i]))
                        yield return _raycastHitBuffer[i];

                yield break;
            }

            for (int t = 0; t < TagComparisions.Count; t++) {
                for (int i = 0; i < _raycastCount; i++) {
                    if (_raycastHitBuffer[i].collider.CompareTag(TagComparisions[t]) && ValidatePlatformEffector(_raycastHitBuffer[i])) {
                        yield return _raycastHitBuffer[i];
                    }
                }
            }
        }
        #endregion

        IEnumerable<Collider2D> FilterColliderTag() {
            if (TagComparisions == null || TagComparisions.Count == 0) {
                for (int i = 0; i < _colliderCount; i++) yield return _colliderBuffer[i];

                yield break;
            }

            for (int t = 0; t < TagComparisions.Count; t++) {
                for (int i = 0; i < _colliderCount; i++) {
                    if (_colliderBuffer[i].CompareTag(TagComparisions[t])) {
                        yield return _colliderBuffer[i];
                    }
                }
            }
        }

#if UNITY_EDITOR
        const float HitPointVisualizeRadius = 0.2f;

        private void OnDrawGizmosSelected() {
            if (Position == null) return;

            bool check = Check();
            Gizmos.color = check ? Color.green : Color.red;

            switch (Type) {
                case CheckType.BoxCast: {
                    //var q = Quaternion.LookRotation(Vector3.forward, Quaternion.Euler(0, 0, 90) * Direction) * Quaternion.AngleAxis(Angle, Vector3.forward);
                    var q = Quaternion.AngleAxis(Angle, Vector3.forward);

                    var halfSize = Size / 2;

                    Vector3 tl = Position.position + q * new Vector3(-halfSize.x, halfSize.y);
                    Vector3 tr = Position.position + q * halfSize;
                    Vector3 bl = Position.position + q * -halfSize;
                    Vector3 br = Position.position + q * new Vector3(halfSize.x, -halfSize.y);

                    Gizmos.DrawLine(tl, tr);
                    Gizmos.DrawLine(bl, br);
                    Gizmos.DrawLine(tl, bl);
                    Gizmos.DrawLine(tr, br);

                    Vector3 dir = Direction * Distance;
                    var tl2 = tl + dir;
                    var tr2 = tr + dir;
                    var bl2 = bl + dir;
                    var br2 = br + dir;

                    Gizmos.DrawLine(tl, tl2);
                    Gizmos.DrawLine(tr, tr2);
                    Gizmos.DrawLine(bl, bl2);
                    Gizmos.DrawLine(br, br2);

                    Gizmos.DrawLine(tl2, tr2);
                    Gizmos.DrawLine(bl2, br2);
                    Gizmos.DrawLine(tl2, bl2);
                    Gizmos.DrawLine(tr2, br2);

                    if (check) {
                        VisualizeRaycastInformations();
                    }
                    break;
                }

                case CheckType.CapsuleCast:
                case CheckType.CircleCast:
                    // Gizmos.

                    if (check) {
                        VisualizeRaycastInformations();
                    }
                    break;

                case CheckType.LineCast:
                    Gizmos.DrawLine(Position.position, LineCastEnd);

                    if (check) {
                        VisualizeRaycastInformations();
                    }
                    break;

                case CheckType.OverlapArea:
                    Gizmos.DrawWireCube(((Vector2)Position.position + OverlapAreaEnd) / 2, OverlapAreaEnd - (Vector2)Position.position);
                    break;

                case CheckType.OverlapBox: {
                    Quaternion q = Quaternion.AngleAxis(Angle, Vector3.forward);

                    var halfSize = Size / 2;

                    Vector3 tl = Position.position + q * new Vector3(-halfSize.x, halfSize.y);
                    Vector3 tr = Position.position + q * halfSize;
                    Vector3 bl = Position.position + q * -halfSize;
                    Vector3 br = Position.position + q * new Vector3(halfSize.x, -halfSize.y);

                    Gizmos.DrawLine(tl, tr);
                    Gizmos.DrawLine(bl, br);
                    Gizmos.DrawLine(tl, bl);
                    Gizmos.DrawLine(tr, br);
                    break;
                }

                case CheckType.OverlapCircle:
                    Gizmos.DrawWireSphere(Position.position, Radius);
                    break;

                case CheckType.OverlapPoint:
                    Gizmos.DrawWireSphere(Position.position, 0.1f);
                    break;

                case CheckType.Raycast:
                    Gizmos.DrawRay(Position.position, Direction * Distance);

                    if (check) {
                        VisualizeRaycastInformations();
                    }
                    break;
            }
        }

        void VisualizeRaycastInformations() {
            for (int i = 0; i < _raycastCount; i++) {
                Gizmos.DrawWireSphere(_raycastHitBuffer[i].point, HitPointVisualizeRadius);
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _raycastCount; i++) {
                Gizmos.DrawRay(_raycastHitBuffer[i].point, _raycastHitBuffer[i].normal);
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