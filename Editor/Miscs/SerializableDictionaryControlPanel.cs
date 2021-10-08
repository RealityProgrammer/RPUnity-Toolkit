using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using RealityProgrammer.UnityToolkit.Editors.Windows;
using RealityProgrammer.UnityToolkit.Editors.Utility;
using RealityProgrammer.CSStandard.Interpreter;

namespace RealityProgrammer.UnityToolkit.Editors.Miscs {
    public class SerializableDictionaryControlPanel {
        static SerializableDictionaryControlPanel() {
            GUIStyle variable = new GUIStyle() {
                border = new RectOffset(1, 1, 10, 10),
                padding = new RectOffset(3, 3, 1, 1),
            };

            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>("Dark/SDCP_NormalBgHeader");
            RPEditorStyleStorage.RegisterStyle("SerializableDictionary.ControlPanel.BackgroundHeader.Dark", variable);

            variable = new GUIStyle(RPEditorStyleStorage.AccessStyle("SerializableDictionary.ControlPanel.BackgroundHeader.Dark"));
            variable.padding.top = 4;
            variable.padding.bottom = 4;
            variable.hover.background = Resources.Load<Texture2D>("Dark/SDCP_HoverBgHeader");

            variable.hover.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.active.background = Resources.Load<Texture2D>("Dark/SDCP_HoldBgHeader");
            variable.active.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.fontStyle = FontStyle.Bold;

            RPEditorStyleStorage.RegisterStyle("SerializableDictionary.ControlPanel.BackgroundHeader.Dark.Button0", variable);

            variable = new GUIStyle {
                border = new RectOffset(1, 1, 0, 1),
            };
            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>("Dark/SDCP_NormalBgBottom");

            RPEditorStyleStorage.RegisterStyle("SerializableDictionary.ControlPanel.BackgroundBottom.Dark", variable);

            variable = new GUIStyle() {
                border = new RectOffset(2, 2, 2, 2),
                fixedWidth = 10,
                fixedHeight = 10,
            };

            variable.normal.background = Resources.Load<Texture2D>("Dark/Searchbar_NormalCancelButton");
            variable.hover.background = Resources.Load<Texture2D>("Dark/Searchbar_HoverCancelButton");

            RPEditorStyleStorage.RegisterStyle("SerializableDictionary.ControlPanel.Searchbar.CancelButton.Dark", variable);
        }

        public class CachedReflectionProperties {
            public IDictionary actualDictionaryInstance;

            public FieldInfo fieldInfo;

            public MethodInfo addCandidateMethod, clearCandidateMethod, containsCandidateMethod;

            public FieldInfo candidateKeyField, candidateValueField;

            public Type[] dictionaryGenericTypes;

            public void InitializeReflectionMembers() {
                if (actualDictionaryInstance == null) {
                    Debug.Log(GetType().Name + ": Something go wrong. Report this if you are reading this");
                    return;
                }

                Type type = actualDictionaryInstance.GetType();
                dictionaryGenericTypes = type.GetGenericArguments();

                addCandidateMethod = type.GetMethod("AddCandidate", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);
                clearCandidateMethod = type.GetMethod("ClearCandidate", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);
                containsCandidateMethod = type.GetMethod("ContainsCandidate", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);

                candidateKeyField = type.GetField("_candidateKey", BindingFlags.NonPublic | BindingFlags.Instance);
                candidateValueField = type.GetField("_candidateValue", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        public Action onPairAdd;

        private CachedReflectionProperties _cached;

        private ControlMode controlMode;

        readonly AnimBool panelFoldout;

        private readonly SerializedProperty _dictionaryProperty;

        private readonly SerializedProperty _keysProperty, _valuesProperty;
        private readonly SerializedProperty _candidateKeyProperty, _candidateValueProperty;

        public Action<ControlMode> onControlModeChanged;

        public SerializableDictionaryControlPanel(SerializedProperty dictionary, FieldInfo fieldInfo) {
            _dictionaryProperty = dictionary;
            _keysProperty = _dictionaryProperty.FindPropertyRelative("_Keys");
            _valuesProperty = _dictionaryProperty.FindPropertyRelative("_Values");

            _candidateKeyProperty = _dictionaryProperty.FindPropertyRelative("_candidateKey");
            _candidateValueProperty = _dictionaryProperty.FindPropertyRelative("_candidateValue");

            panelFoldout = new AnimBool(true);
            panelFoldout.valueChanged.AddListener(() => {
                SerializableDictionaryWindow.WindowInstance.Repaint();
            });

            _cached = new CachedReflectionProperties {
                fieldInfo = fieldInfo,
                actualDictionaryInstance = RPEditorUtility.GetActualInstance<IDictionary>(_dictionaryProperty),
            };
            _cached.InitializeReflectionMembers();

            CheckContainsCandidate();

            onControlModeChanged += (mode) => {
                searchString = string.Empty;
            };
        }

        public void Initialize() {
        }

        bool containsCandidate;
        string searchString;

        public void DrawLayout() {
            var style = RPEditorStyleStorage.AccessStyle("SerializableDictionary.ControlPanel.BackgroundHeader.Dark.Button0");
            var label = new GUIContent(ObjectNames.NicifyVariableName(SerializableDictionaryWindow.WindowInstance.DictionaryProperty.displayName) + " - Control Panel");

            if (GUILayout.Button(label, style, GUILayout.Height(24))) {
                panelFoldout.target = !panelFoldout.value;
            }

            var entryCount = SerializableDictionaryWindow.WindowInstance.Displayer.Count;
            var entryDisplayContent = new GUIContent(entryCount + " Item" + (entryCount != 1 ? "s" : ""));
            var size = EditorStyles.label.CalcSize(entryDisplayContent);
            var lastRect = GUILayoutUtility.GetLastRect();

            EditorGUI.LabelField(new Rect(lastRect.x + lastRect.width - size.x - 7, lastRect.y, size.x, lastRect.height), entryDisplayContent);

            EditorGUILayout.BeginFadeGroup(panelFoldout.faded);

            if (panelFoldout.faded > 0.001f) {
                var bottomBackground = RPEditorStyleStorage.AccessStyle("SerializableDictionary.ControlPanel.BackgroundBottom.Dark");
                var bottomRect = EditorGUILayout.BeginVertical();

                if (Event.current.type == EventType.Repaint) {
                    bottomBackground.Draw(bottomRect, false, false, false, false);
                }
                GUILayout.Space(1);

                var rect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), GUI.skin.FindStyle("IconButton"))) {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Pair Modifier"), controlMode == ControlMode.PairModifier, () => {
                        if (controlMode != ControlMode.PairModifier) {
                            controlMode = ControlMode.PairModifier;
                            onControlModeChanged?.Invoke(controlMode);
                        }
                    });

                    if (_cached.actualDictionaryInstance.Count != 0) {
                        menu.AddItem(new GUIContent("Search"), controlMode == ControlMode.Search, () => {
                            if (controlMode != ControlMode.Search) {
                                controlMode = ControlMode.Search;
                                onControlModeChanged?.Invoke(controlMode);
                            }
                        });
                    } else {
                        menu.AddDisabledItem(new GUIContent("Search"), controlMode == ControlMode.Search);
                    }

                    menu.AddItem(new GUIContent("UI"), controlMode == ControlMode.UI, () => {
                        if (controlMode != ControlMode.UI) {
                            controlMode = ControlMode.UI;
                            onControlModeChanged?.Invoke(controlMode);
                        }
                    });

                    menu.ShowAsContext();
                }

                EditorGUILayout.Space(1);
                EditorGUILayout.EndHorizontal();

                switch (controlMode) {
                    case ControlMode.PairModifier:
                        DoPairModifier();
                        break;

                    case ControlMode.Search:
                        EditorGUILayout.LabelField(new GUIContent("Search Query", "Search for keys based on search program/query\nKeywords:\n  1. __iterator__: current iterating entry.\n  2. __iteratorIndex__: the index of current iterating entry.\nThe program can be created like default C# program, as long as the final result is boolean type.\nEx: __iterator__.IntegerValue > 4"), EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal();

                        GUILayout.Space(5);

                        searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
                        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Text);

                        var buttonStyle = RPEditorStyleStorage.AccessStyle("SerializableDictionary.ControlPanel.Searchbar.CancelButton.Dark");

                        EditorGUILayout.BeginVertical(GUILayout.Width(12), GUILayout.Height(18));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("", buttonStyle)) {
                            searchString = string.Empty;
                            GUI.FocusControl(null);

                            SerializableDictionaryWindow.WindowInstance.SearchInterpreter.Clear();
                            SerializableDictionaryWindow.WindowInstance.Displayer._cached.RefreshIndexLookupList();
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(2);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);

                        if (GUILayout.Button("Search")) {
                            try {
                                SerializableDictionaryWindow.WindowInstance.SearchInterpreter.InitializeProgram(searchString);
                                SerializableDictionaryWindow.WindowInstance.SearchInterpreter.Lexing();

                                SerializableDictionaryWindow.WindowInstance.Displayer._cached.RefreshIndexLookupList();
                            } catch (Exception e) {
                                Debug.LogWarning(e.GetType().Name + ": " + e.Message);
                            }
                        }
                        GUILayout.Space(10);
                        EditorGUILayout.EndHorizontal();
                        break;

                    case ControlMode.UI:
                        SerializableDictionaryWindow.WindowInstance.Displayer.DisplayAmountPerPage = EditorGUILayout.IntSlider("Pair Display Amount", SerializableDictionaryWindow.WindowInstance.Displayer.DisplayAmountPerPage, 5, 15);
                        break;
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFadeGroup();
        }

        void DoPairModifier() {
            EditorGUILayout.LabelField("Add New Pair", EditorStyles.boldLabel);
            var keyType = _cached.dictionaryGenericTypes[0];

            bool safeFlag = false;
            if (keyType.IsArray) {
                safeFlag = keyType.GetElementType().IsSubclassOf(typeof(UnityEngine.Object));
            } else if (keyType.IsGenericTypeDefinition && keyType.GetGenericTypeDefinition() == typeof(List<>)) {
                safeFlag = keyType.GetGenericArguments()[0].IsSubclassOf(typeof(UnityEngine.Object));
            } else {
                safeFlag = keyType.IsSubclassOf(typeof(UnityEngine.Object));
            }

            if (!safeFlag) {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(_candidateKeyProperty, new GUIContent("New Key"));

                if (EditorGUI.EndChangeCheck()) {
                    _candidateKeyProperty.serializedObject.ApplyModifiedProperties();
                    CheckContainsCandidate();
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_candidateValueProperty, new GUIContent("New Value"));

                if (containsCandidate) {
                    EditorGUILayout.HelpBox("Key already exists", MessageType.Error, true);

                    GUI.enabled = false;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Button("Add");
                    GUILayout.Space(10);
                    EditorGUILayout.EndHorizontal();
                    GUI.enabled = true;
                } else {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);

                    if (GUILayout.Button("Add")) {
                        InvokeAddCandidate();
                    }
                    GUILayout.Space(10);
                    EditorGUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck()) {
                    _dictionaryProperty.serializedObject.ApplyModifiedProperties();
                }
            } else {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(_candidateKeyProperty, new GUIContent("New Key"));

                if (EditorGUI.EndChangeCheck()) {
                    _candidateKeyProperty.serializedObject.ApplyModifiedProperties();
                    CheckContainsCandidate();
                }

                if (_candidateKeyProperty.objectReferenceValue) {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_candidateValueProperty, new GUIContent("New Value"));

                    if (containsCandidate) {
                        EditorGUILayout.HelpBox("Key already exists", MessageType.Error, true);

                        GUI.enabled = false;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);

                        GUILayout.Button("Add");

                        GUILayout.Space(10);
                        EditorGUILayout.EndHorizontal();
                        GUI.enabled = true;
                    } else {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);

                        if (GUILayout.Button("Add")) {
                            InvokeAddCandidate();
                        }
                        GUILayout.Space(10);
                        EditorGUILayout.EndHorizontal();
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        _dictionaryProperty.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        private void InvokeAddCandidate() {
            GUI.FocusControl(null);
            _cached.addCandidateMethod.Invoke(_cached.actualDictionaryInstance, null);
            CheckContainsCandidate();

            _cached.clearCandidateMethod.Invoke(_cached.actualDictionaryInstance, null);

            _dictionaryProperty.serializedObject.Update(); // Update the method to prevent the display throw NPE on retrieving indices

            onPairAdd?.Invoke();
        }

        void CheckContainsCandidate() {
            containsCandidate = (bool)_cached.containsCandidateMethod.Invoke(_cached.actualDictionaryInstance, null);
        }

        public float GetHeight() {
            return 100;
        }

        public enum ControlMode {
            PairModifier, Search, UI,
        }
    }
}