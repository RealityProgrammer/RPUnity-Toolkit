using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Core.Miscs;
using RealityProgrammer.UnityToolkit.Editors.Windows;
using RealityProgrammer.UnityToolkit.Editors.Utility;
using System;
using System.Reflection;

namespace RealityProgrammer.UnityToolkit.Editors.Miscs {
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
    public class SerializableDictionary_PropertyDrawer : PropertyDrawer {
        private readonly Dictionary<string, bool> validate = new Dictionary<string, bool>();

        public void Validate(SerializedProperty property) {
            var dictionary = RPEditorUtility.GetActualInstance(fieldInfo, property);
            var entriesField = dictionary.GetType().GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
            var keyType = entriesField.FieldType.GetElementType().GetField("key", BindingFlags.Instance | BindingFlags.Public).FieldType;

            if (RPEditorUtility.IsSerializableByUnity(keyType)) {
                validate[property.propertyPath] = true;
            } else {
                validate[property.propertyPath] = false;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!validate.TryGetValue(property.propertyPath, out bool valid)) {
                Validate(property);
            }

            if (validate[property.propertyPath]) {
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(position, label);

                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                if (GUI.Button(position, "Open Window")) {
                    SerializableDictionaryWindow.InitializeWindow(property, fieldInfo);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (!validate.TryGetValue(property.propertyPath, out bool valid)) {
                Validate(property);
            }

            if (validate[property.propertyPath]) {
                return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
            }

            return 0;
        }
    }
}