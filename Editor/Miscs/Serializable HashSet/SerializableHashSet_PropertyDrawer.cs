using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Core.Miscs;
using RealityProgrammer.UnityToolkit.Editors.Windows.SerializableHashSet;
using RealityProgrammer.UnityToolkit.Editors.Utility;

namespace RealityProgrammer.UnityToolkit.Editors.Miscs {
    [CustomPropertyDrawer(typeof(SerializableHashSet<>))]
    internal class SerializableHashSet_PropertyDrawer : PropertyDrawer {
        public const int LineBreakThreshold = 325;

        private readonly Dictionary<string, bool> validate = new Dictionary<string, bool>();

        public void Initiate(SerializedProperty property) {
            var dictionary = RPEditorUtility.GetActualInstance(property);
            var slots = dictionary.GetType().GetField("m_slots", BindingFlags.NonPublic | BindingFlags.Instance);
            var keyType = slots.FieldType.GetElementType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic).FieldType;

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
                        SerializableHashSetWindow.InitializeWindow(property, fieldInfo);
                    }
                } else {
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

                    if (GUI.Button(position, new GUIContent("Open Window"))) {
                        SerializableHashSetWindow.InitializeWindow(property, fieldInfo);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (!validate.ContainsKey(property.propertyPath)) {
                Initiate(property);
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