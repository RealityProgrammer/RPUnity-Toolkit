using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Core.Miscs;
using RealityProgrammer.UnityToolkit.Core.Utility;

namespace RealityProgrammer.UnityToolkit.Editors.Miscs {
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReference_PropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.serializedObject.isEditingMultipleObjects) {
                GUI.Label(position, "Multiple Edit are not supported (yet).");
                return;
            }

            EditorGUI.PropertyField(position, property.FindPropertyRelative("sceneAsset"));

            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}