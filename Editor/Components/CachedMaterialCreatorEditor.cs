using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using RealityProgrammer.UnityToolkit.Core.Components;
using UnityEngine.Rendering;
using RealityProgrammer.UnityToolkit.Core.Rendering;

namespace RealityProgrammer.Editors {
    [CustomEditor(typeof(CachedMaterialCreator))]
    public class CachedMaterialCreatorEditor : Editor {
        SerializedProperty originalMaterial_Prop, materialKey_Prop, overrideIfExists_Prop, callMethod_Prop, propertyEntries_Prop;
        SerializedProperty localSceneOnly_Prop;

        private ReorderableList _reorderableList;

        private void OnEnable() {
            originalMaterial_Prop = serializedObject.FindProperty("originalMaterial");
            materialKey_Prop = serializedObject.FindProperty("materialKey");
            overrideIfExists_Prop = serializedObject.FindProperty("overrideIfExists");
            callMethod_Prop = serializedObject.FindProperty("callMethod");
            propertyEntries_Prop = serializedObject.FindProperty("propertyEntries");
            localSceneOnly_Prop = serializedObject.FindProperty("localSceneOnly");

            _reorderableList = new ReorderableList(serializedObject, propertyEntries_Prop, true, true, true, true) {
                drawHeaderCallback = (rect) => {
                    EditorGUI.LabelField(rect, "Shader Properties");
                },
                onAddDropdownCallback = (rect, list) => {
                    GenericMenu menu = new GenericMenu();

                    Shader shader = (originalMaterial_Prop.objectReferenceValue as Material).shader;

                    for (int i = 0; i < shader.GetPropertyCount(); i++) {
                        string sPropName = shader.GetPropertyName(i);

                        bool exists = false;

                        for (int j = 0; j < list.serializedProperty.arraySize; j++) {
                            if (list.serializedProperty.GetArrayElementAtIndex(j).FindPropertyRelative(nameof(PropertyModifyEntry.propertyName)).stringValue == sPropName) {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists) {
                            int _i = i;
                            menu.AddItem(new GUIContent(sPropName), false, () => {
                                ShaderPropertyType shaderPropertyType = shader.GetPropertyType(_i);

                                int index = list.serializedProperty.arraySize;
                                list.serializedProperty.arraySize++;
                                list.index = index;

                                var element = list.serializedProperty.GetArrayElementAtIndex(index);

                                element.FindPropertyRelative(nameof(PropertyModifyEntry.propertyName)).stringValue = shader.GetPropertyName(_i);
                                element.FindPropertyRelative(nameof(PropertyModifyEntry.propertyType)).enumValueIndex = (int)shaderPropertyType;
                                element.FindPropertyRelative(nameof(PropertyModifyEntry.foldout)).boolValue = false;

                                string[] propAttrs = shader.GetPropertyAttributes(_i);

                                var attributesProp = element.FindPropertyRelative(nameof(PropertyModifyEntry.attributes));
                                attributesProp.arraySize = propAttrs.Length;
                                for (int i = 0; i < propAttrs.Length; i++) {
                                    attributesProp.GetArrayElementAtIndex(i).stringValue = propAttrs[i];
                                }

                                if (shaderPropertyType == ShaderPropertyType.Range) {
                                    element.FindPropertyRelative(nameof(PropertyModifyEntry.range)).vector2Value = shader.GetPropertyRangeLimits(_i);
                                }

                                element.FindPropertyRelative(nameof(PropertyModifyEntry.propertyFlags)).intValue = (int)shader.GetPropertyFlags(_i);

                                serializedObject.ApplyModifiedProperties();
                            });
                        } else {
                            menu.AddDisabledItem(new GUIContent(sPropName));
                        }
                    }

                    menu.ShowAsContext();
                },
                elementHeightCallback = (index) => {
                    return EditorGUI.GetPropertyHeight(_reorderableList.serializedProperty.GetArrayElementAtIndex(index));
                },
            };

            _reorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) => {
                EditorGUI.PropertyField(rect, _reorderableList.serializedProperty.GetArrayElementAtIndex(index));
            };
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(originalMaterial_Prop);
            if (originalMaterial_Prop.objectReferenceValue != null) {
                EditorGUILayout.PropertyField(materialKey_Prop);

                if (materialKey_Prop.stringValue.Trim() == string.Empty) {
                    EditorGUILayout.HelpBox("Material Key are not supposed to be empty after the white spaces are trimmed", MessageType.Error, true);
                }

                EditorGUILayout.PropertyField(overrideIfExists_Prop);
                EditorGUILayout.PropertyField(callMethod_Prop);

                _reorderableList.DoLayoutList();

                EditorGUILayout.PropertyField(localSceneOnly_Prop);
            }

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}