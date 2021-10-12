using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Core.Components;

namespace RealityProgrammer.UnityToolkit.Editors.Components {
	[CustomEditor(typeof(Collider2DPhysicsChecker))]
	public class Collider2DPhysicsChecker_Editor : Editor
	{
		SerializedProperty positionProperty, checkTypeProperty;

		SerializedProperty sizeProperty, angleProperty, distanceProperty, radiusProperty;
		SerializedProperty layerMaskProperty;

		SerializedProperty checkEveryFrameProperty;

		SerializedProperty startCollisionCallbackProperty, endCollisionCallbackProperty;

		private static Texture2D directionCircleTexture, directionHandleTexture;

		private Collider2DPhysicsChecker component;

		private void OnEnable() {
			positionProperty = serializedObject.FindProperty("<Position>k__BackingField");
			checkTypeProperty = serializedObject.FindProperty("<Type>k__BackingField");
			sizeProperty = serializedObject.FindProperty("<Size>k__BackingField");
			angleProperty = serializedObject.FindProperty("<Angle>k__BackingField");
			distanceProperty = serializedObject.FindProperty("<Distance>k__BackingField");
			layerMaskProperty = serializedObject.FindProperty("<LayerMask>k__BackingField");
			radiusProperty = serializedObject.FindProperty("<Radius>k__BackingField");
			checkEveryFrameProperty = serializedObject.FindProperty("<CheckEveryFrame>k__BackingField");
			startCollisionCallbackProperty = serializedObject.FindProperty("<StartCollisionCallback>k__BackingField");
			endCollisionCallbackProperty = serializedObject.FindProperty("<EndCollisionCallback>k__BackingField");

			component = target as Collider2DPhysicsChecker;

			if (directionCircleTexture == null) directionCircleTexture = Resources.Load<Texture2D>("2DDirectionModifyCircle");
			if (directionHandleTexture == null) directionHandleTexture = Resources.Load<Texture2D>("2DDirectionModifyHandle");
		}

		public override void OnInspectorGUI() {

			EditorGUI.BeginChangeCheck();
			serializedObject.Update();

			EditorGUILayout.PropertyField(positionProperty);

			if (positionProperty.objectReferenceValue != null) {
				EditorGUILayout.PropertyField(checkTypeProperty);
				EditorGUILayout.PropertyField(checkEveryFrameProperty);

				EditorGUILayout.Space(6);
				switch ((Collider2DPhysicsChecker.CheckType)checkTypeProperty.intValue) {
					case Collider2DPhysicsChecker.CheckType.BoxCast:
						EditorGUILayout.LabelField("Box Cast Properties", EditorStyles.boldLabel);

						DoDirectionField();
						EditorGUILayout.PropertyField(sizeProperty);
						EditorGUILayout.PropertyField(angleProperty);
						EditorGUILayout.PropertyField(distanceProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.CapsuleCast:
						EditorGUILayout.LabelField("Capsule Cast Properties", EditorStyles.boldLabel);

						DoDirectionField();
						EditorGUILayout.PropertyField(sizeProperty);
						EditorGUILayout.PropertyField(angleProperty);
						EditorGUILayout.PropertyField(distanceProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.CircleCast:
						EditorGUILayout.LabelField("Circle Cast Properties", EditorStyles.boldLabel);

						DoDirectionField();
						EditorGUILayout.PropertyField(radiusProperty);
						EditorGUILayout.PropertyField(distanceProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.LineCast:
						EditorGUILayout.LabelField("Line Cast Properties", EditorStyles.boldLabel);

						DoDirectionField();
						EditorGUILayout.PropertyField(distanceProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.OverlapArea:
						EditorGUILayout.LabelField("Overlap Area Properties", EditorStyles.boldLabel);

						EditorGUI.BeginChangeCheck();
						var vector = EditorGUILayout.Vector2Field("End Point", component.OverlapAreaEnd);
						if (EditorGUI.EndChangeCheck()) {
							Undo.RecordObject(component, "Change OverlapArea End Point");

							var delta = vector - (Vector2)component.Position.position;

							component.Direction = delta.normalized;
							component.Distance = delta.magnitude;
						}
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.OverlapBox:
						EditorGUILayout.LabelField("Overlap Box Properties", EditorStyles.boldLabel);

						EditorGUILayout.PropertyField(sizeProperty);
						EditorGUILayout.PropertyField(angleProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.OverlapCapsule:
						EditorGUILayout.LabelField("Overlap Capsule Properties", EditorStyles.boldLabel);

						EditorGUILayout.PropertyField(sizeProperty);
						EditorGUILayout.PropertyField(angleProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.OverlapCircle:
						EditorGUILayout.LabelField("Overlap Circle Properties", EditorStyles.boldLabel);

						EditorGUILayout.PropertyField(radiusProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.OverlapPoint:
						EditorGUILayout.LabelField("Overlap Point Properties", EditorStyles.boldLabel);

						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					case Collider2DPhysicsChecker.CheckType.Raycast:
						EditorGUILayout.LabelField("Raycast Properties", EditorStyles.boldLabel);

						DoDirectionField();
						EditorGUILayout.PropertyField(distanceProperty);
						EditorGUILayout.PropertyField(layerMaskProperty);
						break;

					default:
						EditorGUILayout.HelpBox("Undefined Check Type", MessageType.Error, true);
						break;
				}

				startCollisionCallbackProperty.isExpanded = EditorGUILayout.Foldout(startCollisionCallbackProperty.isExpanded, "Callbacks");
				if (startCollisionCallbackProperty.isExpanded) {
					EditorGUILayout.PropertyField(startCollisionCallbackProperty);
					EditorGUILayout.PropertyField(endCollisionCallbackProperty);
				}

				if (Application.isPlaying) {
					GUI.enabled = false;
					EditorGUILayout.Toggle("Result", component.Result);
					GUI.enabled = true;
				}
			}

			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(target);
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void DoDirectionField() {
			EditorGUILayout.LabelField("Direction");
			var rect = EditorGUILayout.GetControlRect(GUILayout.Height(128));

			EditorGUI.BeginChangeCheck();
			var dir = DoCircleDirectionControl(rect, component.Direction);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(component, "Change Direction");
				component.Direction = dir;
			}

			EditorGUILayout.Space(2);

			EditorGUI.BeginChangeCheck();

			float deg = Mathf.Atan2(component.Direction.y, component.Direction.x) * Mathf.Rad2Deg;
			float angle = EditorGUILayout.FloatField("Angle (Degree)", deg < 0 ? 360 + deg : deg) * Mathf.Deg2Rad;
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(component, "Change Direction");
				component.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			}
		}

		private Vector2 DoCircleDirectionControl(Rect rect, Vector2 input) {
			var evt = Event.current;

			GUI.DrawTexture(rect, directionCircleTexture, ScaleMode.ScaleToFit);

			var id = GUIUtility.GetControlID(FocusType.Passive);

			// float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
			// GUIUtility.RotateAroundPivot(-angle, rect.center);
			// GUI.DrawTexture(rect, directionHandleTexture, ScaleMode.ScaleToFit);
			// GUIUtility.RotateAroundPivot(angle, rect.center);

			if (rect.Contains(evt.mousePosition)) {
				switch (evt.GetTypeForControl(id)) {
					case EventType.MouseDown:
						if (evt.button == 0) {
							GUIUtility.hotControl = id;
							evt.Use();
						}
						break;

					case EventType.MouseUp:
						if (GUIUtility.hotControl == id) {
							GUIUtility.hotControl = 0;
							evt.Use();
						}
						break;

					case EventType.MouseDrag:
						if (GUIUtility.hotControl == id && evt.button == 0) {
							var normalize = (evt.mousePosition - rect.center).normalized;
							input = new Vector2(normalize.x, -normalize.y);

							GUI.changed = true;

							evt.Use();
						}
						break;
				}
			}
			
			float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
			GUIUtility.RotateAroundPivot(-angle, rect.center);
			GUI.DrawTexture(rect, directionHandleTexture, ScaleMode.ScaleToFit);
			GUIUtility.RotateAroundPivot(angle, rect.center);

			return input;
		}
	}
}