using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Core.Miscs;
using RealityProgrammer.UnityToolkit.Editors.Windows.SerializableDictionary;
using RealityProgrammer.UnityToolkit.Editors.Utility;
using System;
using System.Reflection;

namespace RealityProgrammer.UnityToolkit.Editors.Miscs {
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
    internal class SerializableDictionary_PropertyDrawer : PropertyDrawer {
        public const int LineBreakThreshold = 325;
        private readonly Dictionary<string, bool> validate = new Dictionary<string, bool>();

        public void Validate(SerializedProperty property) {
            // Do validate like this instead of SerializedProperty.FindPropertyRelative with GetArrayElementAtIndex to prevent Out of Range
            var dictionary = RPEditorUtility.GetActualInstance(property);
            var entriesField = dictionary.GetType().GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
            var keyType = entriesField.FieldType.GetElementType().GetField("key", BindingFlags.Instance | BindingFlags.Public).FieldType;

            if (RPEditorUtility.IsSerializableByUnity(keyType)) {
                validate[property.propertyPath] = true;
            } else {
                validate[property.propertyPath] = false;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (validate[property.propertyPath]) {
                var prefixRect = EditorGUI.PrefixLabel(position, label);

                prefixRect.height = EditorGUIUtility.singleLineHeight;
                position.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.LabelField(position, label);

                if (Screen.width >= LineBreakThreshold) {
                    if (GUI.Button(prefixRect, new GUIContent("Open Window"))) {
                        SerializableDictionaryWindow.InitializeWindow(property, fieldInfo);
                    }
                } else {
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

                    if (GUI.Button(position, new GUIContent("Open Window"))) {
                        SerializableDictionaryWindow.InitializeWindow(property, fieldInfo);
                    }
                }

                //position.height = EditorGUIUtility.singleLineHeight;
                //EditorGUI.LabelField(position, label);

                //position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                //if (GUI.Button(position, "Open Window")) {
                //    SerializableDictionaryWindow.InitializeWindow(property, fieldInfo);
                //}
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (!validate.ContainsKey(property.propertyPath)) {
                Validate(property);
            }

            if (validate[property.propertyPath]) {
                if (Screen.width >= LineBreakThreshold) {
                    return EditorGUIUtility.singleLineHeight;
                }

                return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
            }

            return 0;
        }
    }
}