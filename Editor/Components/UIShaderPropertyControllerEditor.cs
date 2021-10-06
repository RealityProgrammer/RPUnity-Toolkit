using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;
using RealityProgrammer.UnityToolkit.Core.Components;
using RealityProgrammer.UnityToolkit.Core.Rendering;

namespace RealityProgrammer.Editors {
    [CustomEditor(typeof(UIShaderPropertyController))]
    public class UIShaderPropertyControllerEditor : Editor {
        private ReorderableList _reorderableList;

        SerializedProperty targetProp;

        UIShaderPropertyController inspectingTarget;

        private void OnEnable() {
            inspectingTarget = (UIShaderPropertyController)target;

            targetProp = serializedObject.FindProperty("target");

            _reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("entries"), true, true, true, true) {
                drawHeaderCallback = (rect) => {
                    EditorGUI.LabelField(rect, "Shader Properties");
                },
                onAddDropdownCallback = (rect, list) => {
                    GenericMenu menu = new GenericMenu();

                    UIMaterialInstantiator matInst = targetProp.objectReferenceValue as UIMaterialInstantiator;
                    Graphic renderer = matInst.RetrieveGraphic();
                    Shader shader = renderer.material.shader;

                    for (int i = 0; i < shader.GetPropertyCount(); i++) {
                        string sPropName = shader.GetPropertyName(i);

                        bool exists = false;
                        for (int j = 0; j < inspectingTarget.entries.Length; j++) {
                            if (inspectingTarget.entries[j].propertyName == sPropName) {
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

            EditorGUILayout.PropertyField(targetProp);
            if (targetProp.objectReferenceValue) {
                _reorderableList.DoLayoutList();
            }

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}