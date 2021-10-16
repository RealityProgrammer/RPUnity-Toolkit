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

namespace RealityProgrammer.UnityToolkit.Editors.Windows.SerializableDictionary {
    internal class SerializableDictionaryControlPanel {
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

        private readonly SerializedProperty _candidateKeyProperty, _candidateValueProperty;

        public Action<ControlMode> onControlModeChanged;

        public SerializableDictionaryControlPanel(SerializedProperty dictionary, FieldInfo fieldInfo) {
            _dictionaryProperty = dictionary;

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

        bool containsCandidate;
        string searchString;

        private int m_HeaderButtonID;
        public void DoHeaderButton() {
            var style = RPEditorStyleStorage.AccessStyle("BuiltIn.Dark.Button.DarkGradient_0");
            var label = new GUIContent(ObjectNames.NicifyVariableName(SerializableDictionaryWindow.WindowInstance.DictionaryProperty.displayName) + " - Control Panel");

            var e = Event.current;
            int id = GUIUtility.GetControlID(label, FocusType.Passive);

            if (GUILayout.Button(label, style, GUILayout.Height(24))) {
                panelFoldout.target = !panelFoldout.value;
            }

            if (e.type == EventType.MouseMove) {
                if (GUILayoutUtility.GetLastRect().Contains(e.mousePosition)) {
                    if (m_HeaderButtonID != id) {
                        m_HeaderButtonID = id;

                        SerializableDictionaryWindow.WindowInstance.Repaint();
                    }
                } else {
                    if (m_HeaderButtonID == id) {
                        m_HeaderButtonID = 0;

                        SerializableDictionaryWindow.WindowInstance.Repaint();
                    }
                }
            }

            var entryCount = SerializableDictionaryWindow.WindowInstance.Displayer.Count;
            var entryDisplayContent = new GUIContent(entryCount + " Item" + (entryCount != 1 ? "s" : ""));
            var size = EditorStyles.label.CalcSize(entryDisplayContent);
            var lastRect = GUILayoutUtility.GetLastRect();

            EditorGUI.LabelField(new Rect(lastRect.x + lastRect.width - size.x - 7, lastRect.y, size.x, lastRect.height), entryDisplayContent);
        }

        public void DrawLayout() {
            DoHeaderButton();

            EditorGUILayout.BeginFadeGroup(panelFoldout.faded);

            if (panelFoldout.faded > 0.001f) {
                var bottomBackground = RPEditorStyleStorage.AccessStyle("BuiltIn.Dark.Background.BottomConnect_0");
                var bottomRect = EditorGUILayout.BeginVertical();

                if (Event.current.type == EventType.Repaint) {
                    bottomBackground.Draw(bottomRect, false, false, false, false);
                }

                GUILayout.Space(1);

                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), GUI.skin.FindStyle("IconButton"))) {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Pair Modifier"), controlMode == ControlMode.PairModifier, () => {
                        if (controlMode != ControlMode.PairModifier) {
                            controlMode = ControlMode.PairModifier;
                            onControlModeChanged?.Invoke(controlMode);
                        }
                    });

                    if (_cached.actualDictionaryInstance.Count > 5) {
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

                        var buttonStyle = RPEditorStyleStorage.AccessStyle("BuiltIn.Dark.Button.SearchbarCancel");

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
                        SerializableDictionaryWindow.WindowInstance.Displayer.DisplayAmountPerPage = EditorGUILayout.IntSlider("Pair Display Amount", SerializableDictionaryWindow.WindowInstance.Displayer.DisplayAmountPerPage, SerializableDictionaryWindow.DisplayAmountRange.x, SerializableDictionaryWindow.DisplayAmountRange.y);
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
                DoCandidateKeyField();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_candidateValueProperty, new GUIContent("New Value"));

                DoAddButton();

                if (EditorGUI.EndChangeCheck()) {
                    _dictionaryProperty.serializedObject.ApplyModifiedProperties();
                }
            } else {
                DoCandidateKeyField();

                if (_candidateKeyProperty.objectReferenceValue) {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(_candidateValueProperty, new GUIContent("New Value"));

                    DoAddButton();

                    if (EditorGUI.EndChangeCheck()) {
                        _dictionaryProperty.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        void DoAddButton() {
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
        }

        void DoCandidateKeyField() {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_candidateKeyProperty, new GUIContent("New Key"));

            if (EditorGUI.EndChangeCheck()) {
                _candidateKeyProperty.serializedObject.ApplyModifiedProperties();
                CheckContainsCandidate();
            }
        }

        public void CloseWindow() {
            //ClearCandidate();

            //_dictionaryProperty.serializedObject.ApplyModifiedProperties();
        }

        internal void InvokeAddCandidate() {
            GUI.FocusControl(null);
            _cached.addCandidateMethod.Invoke(_cached.actualDictionaryInstance, null);
            ClearCandidate();

            _dictionaryProperty.serializedObject.Update(); // Update the method to prevent the displayer throw NPE on retrieving indices

            onPairAdd?.Invoke();
        }

        internal void ClearCandidate() {
            _cached.clearCandidateMethod.Invoke(_cached.actualDictionaryInstance, null);
        }

        internal void CheckContainsCandidate() {
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