using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Core.Rendering;

namespace RealityProgrammer.UnityToolkit.Editors {
    [CustomPropertyDrawer(typeof(PropertyModifyEntry))]
    public class PropertyModifyEntry_PropDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            position.height = EditorGUIUtility.singleLineHeight;

            GUI.enabled = false;
            EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(PropertyModifyEntry.propertyName)));
            GUI.enabled = true;

            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

            var propertyTypeProp = property.FindPropertyRelative(nameof(PropertyModifyEntry.propertyType));
            var attributesProp = property.FindPropertyRelative(nameof(PropertyModifyEntry.attributes));
            var propertyFlagsProp = property.FindPropertyRelative(nameof(PropertyModifyEntry.propertyFlags));

            switch ((ShaderPropertyType)propertyTypeProp.enumValueIndex) {
                case ShaderPropertyType.Float:
                    if (ContainsAttribute(attributesProp, "MaterialToggle")) {
                        var prop = property.FindPropertyRelative(nameof(PropertyModifyEntry.floatValue));
                        bool boolean = prop.floatValue != 0;

                        prop.floatValue = EditorGUI.Toggle(position, "Toggle", boolean) ? 1 : 0;
                    } else {
                        EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(PropertyModifyEntry.floatValue)));
                    }
                    break;

                case ShaderPropertyType.Range: {
                    var prop = property.FindPropertyRelative(nameof(PropertyModifyEntry.floatValue));
                    var rangeProp = property.FindPropertyRelative(nameof(PropertyModifyEntry.range));

                    EditorGUI.Slider(position, prop, rangeProp.vector2Value.x, rangeProp.vector2Value.y);
                    break;
                }

                case ShaderPropertyType.Color:
                    var colorProp = property.FindPropertyRelative(nameof(PropertyModifyEntry.colorValue));

                    if (((ShaderPropertyFlags)propertyFlagsProp.intValue & ShaderPropertyFlags.HDR) == ShaderPropertyFlags.HDR) {
                        colorProp.colorValue = EditorGUI.ColorField(position, new GUIContent("Color"), colorProp.colorValue, true, true, true);
                    } else {
                        EditorGUI.PropertyField(position, colorProp);
                    }
                    break;

                case ShaderPropertyType.Vector:
                    var vectorProp = property.FindPropertyRelative(nameof(PropertyModifyEntry.vectorValue));
                    EditorGUI.PropertyField(position, vectorProp.FindPropertyRelative("x"));
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, vectorProp.FindPropertyRelative("y"));
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, vectorProp.FindPropertyRelative("z"));
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(position, vectorProp.FindPropertyRelative("w"));
                    break;

                case ShaderPropertyType.Texture:
                    EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(PropertyModifyEntry.textureValue)));
                    break;
            }
        }

        private bool ContainsAttribute(SerializedProperty attributeArray, string attr) {
            for (int i = 0; i < attributeArray.arraySize; i++) {
                if (attributeArray.GetArrayElementAtIndex(i).stringValue == attr) {
                    return true;
                }
            }

            return false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = EditorGUIUtility.singleLineHeight;

            switch ((ShaderPropertyType)property.FindPropertyRelative(nameof(PropertyModifyEntry.propertyType)).enumValueIndex) {
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                case ShaderPropertyType.Color:
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;

                case ShaderPropertyType.Vector:
                    height += EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
                    break;

                case ShaderPropertyType.Texture:
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            return height;
        }
    }
}