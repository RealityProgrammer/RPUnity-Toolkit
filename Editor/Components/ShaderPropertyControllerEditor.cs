using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using RealityProgrammer.UnityToolkit.Core.Rendering;

namespace RealityProgrammer.UnityToolkit.Editors {
    [CustomEditor(typeof(ShaderPropertyController))]
    public class ShaderPropertyControllerEditor : Editor {
        private ReorderableList _reorderableList;

        SerializedProperty targetRendererProp;

        ShaderPropertyController inspectingTarget;

        private static readonly GUIContent noProperty = new GUIContent("No Property");

        private void OnEnable() {
            inspectingTarget = (ShaderPropertyController)target;

            targetRendererProp = serializedObject.FindProperty("targetRenderer");

            _reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("entries")) {
                drawHeaderCallback = (rect) => {
                    EditorGUI.LabelField(rect, "Shader Properties");
                },
                onAddDropdownCallback = (rect, list) => {
                    GenericMenu menu = new GenericMenu();

                    Renderer renderer = targetRendererProp.objectReferenceValue as Renderer;
                    Shader shader = renderer.sharedMaterial.shader;

                    bool isMenuEmpty = true;
                    for (int i = 0; i < shader.GetPropertyCount(); i++) {
                        string sPropName = shader.GetPropertyName(i);
                        var attributes = shader.GetPropertyAttributes(i);

                        if (!attributes.Contains("PerRendererData")) continue;

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
                                isMenuEmpty = false;

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
                            isMenuEmpty = false;
                            menu.AddDisabledItem(new GUIContent(sPropName));
                        }
                    }

                    if (isMenuEmpty) {
                        menu.AddDisabledItem(noProperty);
                    }

                    menu.ShowAsContext();
                },
                elementHeightCallback = (index) => {
                    return EditorGUI.GetPropertyHeight(_reorderableList.serializedProperty.GetArrayElementAtIndex(index));
                }
            };

            _reorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) => {
                EditorGUI.PropertyField(rect, _reorderableList.serializedProperty.GetArrayElementAtIndex(index));
            };
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(targetRendererProp);
            if (targetRendererProp.objectReferenceValue) {
                _reorderableList.DoLayoutList();

                Material sharedMaterial = (targetRendererProp.objectReferenceValue as Renderer).sharedMaterial;
                if (sharedMaterial != null) {
                    if (GUI.Button(EditorGUILayout.GetControlRect(), "Override with current material setting")) {
                        bool apply = EditorUtility.DisplayDialog("New properties override", "Current properties will be discarded and will be replaced by exact settings of Renderer's material (Index 0) properties", "OK", "Cancel");

                        if (apply) {
                            Material mainMaterial = (targetRendererProp.objectReferenceValue as Renderer).sharedMaterial;
                            Shader shader = mainMaterial.shader;

                            inspectingTarget.entries = new PropertyModifyEntry[shader.GetPropertyCount()];

                            for (int i = 0; i < inspectingTarget.entries.Length; i++) {
                                var newEntry = new PropertyModifyEntry();

                                newEntry.propertyName = shader.GetPropertyName(i);
                                newEntry.propertyType = shader.GetPropertyType(i);
                                newEntry.attributes = shader.GetPropertyAttributes(i);
                                newEntry.propertyFlags = shader.GetPropertyFlags(i);

                                switch (newEntry.propertyType) {
                                    case ShaderPropertyType.Float:
                                        newEntry.floatValue = sharedMaterial.GetFloat(newEntry.propertyName);
                                        break;

                                    case ShaderPropertyType.Range:
                                        newEntry.range = shader.GetPropertyRangeLimits(i);
                                        newEntry.floatValue = sharedMaterial.GetFloat(newEntry.propertyName);
                                        break;

                                    case ShaderPropertyType.Color:
                                        newEntry.colorValue = sharedMaterial.GetColor(newEntry.propertyName);
                                        break;

                                    case ShaderPropertyType.Texture:
                                        newEntry.textureValue = sharedMaterial.GetTexture(newEntry.propertyName);
                                        break;

                                    case ShaderPropertyType.Vector:
                                        newEntry.vectorValue = sharedMaterial.GetVector(newEntry.propertyName);
                                        break;
                                }

                                inspectingTarget.entries[i] = newEntry;
                            }

                            GUI.changed = true;
                        }
                    }
                }

                if (EditorGUI.EndChangeCheck()) {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}