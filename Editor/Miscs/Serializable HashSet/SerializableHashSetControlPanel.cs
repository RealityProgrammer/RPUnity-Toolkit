using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using RealityProgrammer.UnityToolkit.Editors.Utility;

namespace RealityProgrammer.UnityToolkit.Editors.Windows.SerializableHashSet {
    internal class SerializableHashSetControlPanel {
        internal class CachedReflectionProperties {
            public FieldInfo fieldInfo;

            public object actualHashSetInstance;

            public MethodInfo addCandidateMethod, clearCandidateMethod, containsCandidateMethod;

            public void InitializeReflectionMembers() {
                Type hashSetType = actualHashSetInstance.GetType();

                addCandidateMethod = hashSetType.GetMethod("AddCandidate", BindingFlags.NonPublic | BindingFlags.Instance);
                clearCandidateMethod = hashSetType.GetMethod("ClearCandidate", BindingFlags.NonPublic | BindingFlags.Instance);
                containsCandidateMethod = hashSetType.GetMethod("ContainsCandidate", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }
        public enum ControlMode {
            Main, UI, Search
        }
        public ControlMode controlMode;

        private SerializedProperty _hashSetProperty;
        private SerializedProperty _slotsProperty, _candidateValueProperty;

        private AnimBool panelFoldout;
        private CachedReflectionProperties _cached;

        public Action onValueAdded;

        public SerializableHashSetControlPanel(SerializedProperty dictionary, FieldInfo fieldInfo) {
            _hashSetProperty = dictionary;
            _slotsProperty = _hashSetProperty.FindPropertyRelative("m_slots");

            _candidateValueProperty = _hashSetProperty.FindPropertyRelative("_candidateValue");

            panelFoldout = new AnimBool(true);
            panelFoldout.valueChanged.AddListener(() => {
                SerializableHashSetWindow.WindowInstance.Repaint();
            });

            _cached = new CachedReflectionProperties {
                fieldInfo = fieldInfo,

                actualHashSetInstance = RPEditorUtility.GetActualInstance(_hashSetProperty),
            };
            _cached.InitializeReflectionMembers();

            CheckContainsCandidate();
        }

        private int m_HeaderButtonID;
        public void DoHeaderButton() {
            var style = RPEditorStyleStorage.AccessStyle("BuiltIn.Dark.Button.DarkGradient_0");
            var label = new GUIContent(ObjectNames.NicifyVariableName(SerializableHashSetWindow.WindowInstance.HashSetProperty.displayName) + " - Control Panel");

            var e = Event.current;
            int id = GUIUtility.GetControlID(label, FocusType.Passive);

            if (GUILayout.Button(label, style, GUILayout.Height(24))) {
                panelFoldout.target = !panelFoldout.value;
            }

            if (e.type == EventType.MouseMove) {
                if (GUILayoutUtility.GetLastRect().Contains(e.mousePosition)) {
                    if (m_HeaderButtonID != id) {
                        m_HeaderButtonID = id;

                        SerializableHashSetWindow.WindowInstance.Repaint();
                    }
                } else {
                    if (m_HeaderButtonID == id) {
                        m_HeaderButtonID = 0;

                        SerializableHashSetWindow.WindowInstance.Repaint();
                    }
                }
            }

            var entryCount = SerializableHashSetWindow.WindowInstance.Displayer.Count;
            var entryDisplayContent = new GUIContent(entryCount + " Item" + (entryCount != 1 ? "s" : ""));
            var size = EditorStyles.label.CalcSize(entryDisplayContent);
            var lastRect = GUILayoutUtility.GetLastRect();

            EditorGUI.LabelField(new Rect(lastRect.x + lastRect.width - size.x - 7, lastRect.y, size.x, lastRect.height), entryDisplayContent);
        }

        private string searchString;
        public void DrawLayout() {
            DoHeaderButton();

            EditorGUILayout.BeginFadeGroup(panelFoldout.faded);

            if (panelFoldout.faded > 0.001) {
                var bottomBackground = RPEditorStyleStorage.AccessStyle("BuiltIn.Dark.Background.BottomConnect_0");
                var bottomRect = EditorGUILayout.BeginVertical();

                if (Event.current.type == EventType.Repaint) {
                    bottomBackground.Draw(bottomRect, false, false, false, false);
                }

                EditorGUILayout.Space(1);

                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), GUI.skin.FindStyle("IconButton"))) {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Main"), controlMode == ControlMode.Main, () => {
                        controlMode = ControlMode.Main;
                    });

                    menu.AddItem(new GUIContent("Search"), controlMode == ControlMode.Search, () => {
                        controlMode = ControlMode.Search;
                    });

                    menu.AddItem(new GUIContent("UI"), controlMode == ControlMode.UI, () => {
                        controlMode = ControlMode.UI;
                    });

                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();

                switch (controlMode) {
                    case ControlMode.Main:
                        DrawMainInterface();
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

                            SerializableHashSetWindow.WindowInstance.SearchInterpreter.Clear();
                            SerializableHashSetWindow.WindowInstance.Displayer._cached.RefreshIndexLookupList();
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(2);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);

                        if (GUILayout.Button("Search")) {
                            try {
                                SerializableHashSetWindow.WindowInstance.SearchInterpreter.InitializeProgram(searchString);
                                SerializableHashSetWindow.WindowInstance.SearchInterpreter.Lexing();

                                SerializableHashSetWindow.WindowInstance.Displayer._cached.RefreshIndexLookupList();
                            } catch (Exception e) {
                                Debug.LogWarning(e.GetType().Name + ": " + e.Message);
                            }
                        }
                        GUILayout.Space(10);
                        EditorGUILayout.EndHorizontal();
                        break;

                    case ControlMode.UI:
                        SerializableHashSetWindow.WindowInstance.Displayer.DisplayAmountPerPage = EditorGUILayout.IntSlider("Pair Display Amount", SerializableHashSetWindow.WindowInstance.Displayer.DisplayAmountPerPage, SerializableHashSetWindow.DisplayAmountRange.x, SerializableHashSetWindow.DisplayAmountRange.y);
                        SerializableHashSetWindow.DebugMode = EditorGUILayout.Toggle("Debug Mode", SerializableHashSetWindow.DebugMode);
                        break;
                }

                EditorGUILayout.Space(3);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFadeGroup();
        }

        void DrawMainInterface() {
            EditorGUILayout.LabelField("Add New Value", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_candidateValueProperty);
            if (EditorGUI.EndChangeCheck()) {
                _candidateValueProperty.serializedObject.ApplyModifiedProperties();
                CheckContainsCandidate();
            }

            if (containsCandidate) {
                EditorGUILayout.HelpBox("Value already exists inside the HashSet", MessageType.Error, true);
            } else {
                if (GUI.Button(EditorGUILayout.GetControlRect(), "Add")) {
                    GUI.FocusControl(null);

                    InvokeAddCandidate();

                    onValueAdded?.Invoke();
                }
            }

            EditorGUILayout.Space(3);
        }

        public void InvokeAddCandidate() {
            _cached.addCandidateMethod.Invoke(_cached.actualHashSetInstance, null);
            _cached.clearCandidateMethod.Invoke(_cached.actualHashSetInstance, null);

            _hashSetProperty.serializedObject.Update();
        }

        private bool containsCandidate;
        public void CheckContainsCandidate() {
            containsCandidate = (bool)_cached.containsCandidateMethod.Invoke(_cached.actualHashSetInstance, null);
        }
    }
}