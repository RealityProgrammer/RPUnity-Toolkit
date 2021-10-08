using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Core.Rendering;
using UnityEngine.Rendering;

namespace RealityProgrammer.UnityToolkit.Editors {
    [CustomEditor(typeof(SubspriteRectCalculator))]
    public class SubspriteRectCalculatorEditor : Editor {
        SerializedProperty propertyNameProp;
        SerializedProperty targetRendererProp;

        private void OnEnable() {
            propertyNameProp = serializedObject.FindProperty("propertyName");
            targetRendererProp = serializedObject.FindProperty("targetRenderer");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(targetRendererProp);

            if (targetRendererProp.objectReferenceValue != null) {
                SpriteRenderer sprRenderer = targetRendererProp.objectReferenceValue as SpriteRenderer;

                if (ValidateSpriteRenderer(sprRenderer)) {
                    Shader shader = sprRenderer.sharedMaterial.shader;

                    EditorGUILayout.LabelField("Property", EditorStyles.boldLabel);

                    string propertyNameProp_V = propertyNameProp.stringValue;
                    bool nameIsValid = CheckPropertyNameIsValid(propertyNameProp_V, shader);

                    if (!nameIsValid) {
                        EditorGUILayout.HelpBox("Cannot find property \"" + propertyNameProp_V + "\" in material's shader", MessageType.Error, true);
                    }

                    List<string> vectorProperties = new List<string>();
                    RetrieveVectorProperties(vectorProperties, sprRenderer.sharedMaterial.shader);

                    if (vectorProperties.Count == 0) {
                        EditorGUILayout.HelpBox("Cannot find any vector property with the [PerRendererData] attribute", MessageType.Warning, true);
                    } else if (vectorProperties.Count == 1) {
                        var controlRect = EditorGUILayout.GetControlRect();

                        if (GUI.Button(controlRect, string.IsNullOrEmpty(propertyNameProp_V) ? "<Empty Property>" : (nameIsValid ? propertyNameProp_V : "<Invalid Property>"))) {
                            propertyNameProp.stringValue = vectorProperties[0];
                        }
                    } else {
                        var controlRect = EditorGUILayout.GetControlRect();

                        if (GUI.Button(controlRect, string.IsNullOrEmpty(propertyNameProp_V) ? "<Empty Property>" : (nameIsValid ? propertyNameProp_V : "<Invalid Property>"))) {
                            GenericMenu menu = new GenericMenu();

                            for (int i = 0; i < vectorProperties.Count; i++) {
                                var _i = i;
                                menu.AddItem(new GUIContent(vectorProperties[i]), propertyNameProp_V == vectorProperties[i], () => {
                                    (target as SubspriteRectCalculator).propertyName = vectorProperties[_i];
                                });
                            }

                            menu.ShowAsContext();
                        }
                    }

                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(propertyNameProp);
                    GUI.enabled = true;
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private bool ValidateSpriteRenderer(SpriteRenderer sprRenderer, bool helpbox = true) {
            if (sprRenderer.sprite == null) {
                if (helpbox) {
                    EditorGUILayout.HelpBox("Cannot calculate subsprite rect of a SpriteRenderer with empty sprite property", MessageType.Error, true);
                }

                return false;
            }

            if (sprRenderer.sharedMaterial == null) {
                if (helpbox) {
                    EditorGUILayout.HelpBox("Cannot calculate subsprite rect of a SpriteRenderer with empty material property", MessageType.Error, true);
                }

                return false;
            }

            return true;
        }

        private void RetrieveVectorProperties(List<string> properties, Shader shader) {
            for (int i = 0; i < shader.GetPropertyCount(); i++) {
                if (shader.GetPropertyType(i) == ShaderPropertyType.Vector) {
                    if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.PerRendererData) == ShaderPropertyFlags.PerRendererData) {
                        properties.Add(shader.GetPropertyName(i));
                    }
                }
            }
        }

        private bool CheckPropertyNameIsValid(string name, Shader shader) {
            for (int i = 0; i < shader.GetPropertyCount(); i++) {
                if (shader.GetPropertyName(i) == name) {
                    if (shader.GetPropertyType(i) == ShaderPropertyType.Vector) {
                        if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.PerRendererData) == ShaderPropertyFlags.PerRendererData) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}