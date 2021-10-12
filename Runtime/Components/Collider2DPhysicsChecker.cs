using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    public class Collider2DPhysicsChecker : MonoBehaviour {
        [field: SerializeField] public Transform Position { get; set; }

        [field: SerializeField] public Vector2 Size { get; set; } = Vector2.one;
        [field: SerializeField] public Vector2 Direction { get; set; } = Vector2.zero;
        [field: SerializeField] public float Distance { get; set; } = 1;
        [field: SerializeField] public float Angle { get; set; }
        [field: SerializeField] public float Radius { get; set; } = 1;

        [field: SerializeField] public LayerMask LayerMask { get; set; }

        [field: SerializeField] public CheckType Type { get; set; }

        [field: SerializeField] public bool CheckEveryFrame { get; set; } = true;

        [field: SerializeField] public UnityEvent StartCollisionCallback { get; set; }
        [field: SerializeField] public UnityEvent EndCollisionCallback { get; set; }

        bool result;

        private void Update() {
            if (CheckEveryFrame) {
                ManualCheck();
            }
        }

        private static RaycastHit2D[] _raycastHitBuffer = new RaycastHit2D[1];
        private static Collider2D[] _colliderBuffer = new Collider2D[1];

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
                case CheckType.BoxCast: return Physics2D.BoxCastNonAlloc(Position.position, Size, Angle, Direction, _raycastHitBuffer, Distance, LayerMask) > 0;
                case CheckType.CapsuleCast: return Physics2D.CapsuleCastNonAlloc(Position.position, Size, CapsuleDirection2D.Horizontal, Angle, Direction, _raycastHitBuffer, Distance, LayerMask) > 0;
                case CheckType.CircleCast: return Physics2D.CircleCastNonAlloc(Position.position, Radius, Direction, _raycastHitBuffer, Distance, LayerMask) > 0;
                case CheckType.LineCast: return Physics2D.LinecastNonAlloc(Position.position, LineCastEnd, _raycastHitBuffer, LayerMask) > 0;
                case CheckType.OverlapArea: return Physics2D.OverlapAreaNonAlloc(Position.position, OverlapAreaEnd, _colliderBuffer, LayerMask) > 0;
                case CheckType.OverlapBox: return Physics2D.OverlapBoxNonAlloc(Position.position, Size, Angle, _colliderBuffer, LayerMask) > 0;
                case CheckType.OverlapCapsule: return Physics2D.OverlapCapsuleNonAlloc(Position.position, Size, CapsuleDirection2D.Horizontal, Angle, _colliderBuffer, LayerMask) > 0;
                case CheckType.OverlapCircle: return Physics2D.OverlapCircleNonAlloc(Position.position, Radius, _colliderBuffer, LayerMask) > 0;
                case CheckType.OverlapPoint: return Physics2D.OverlapPointNonAlloc(Position.position, _colliderBuffer, LayerMask) > 0;
                case CheckType.Raycast: return Physics2D.Raycast(Position.position, Direction, Distance, LayerMask).collider != null;
                default: Debug.LogError("Unsupported check type: " + Type); return false;
            }
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