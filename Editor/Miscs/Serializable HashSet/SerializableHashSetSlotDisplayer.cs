using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using RealityProgrammer.UnityToolkit.Editors.Utility;
using RealityProgrammer.UnityToolkit.Core.Utility;

namespace RealityProgrammer.UnityToolkit.Editors.Windows.SerializableHashSet {
    internal class SerializableHashSetSlotDisplayer {
        internal class CachedReflectionProperties {
            public object actualHashSetInstance;

            public FieldInfo fieldInfo;

            public Type[] dictionaryGenericTypes;

            public MethodInfo removeMethod, getIndexLookupListMethod, getValueLookupListMethod, getSlotHashCodesMethod;

            public List<int> indexLookupList, hashCodeList;
            public List<object> valueLookupList;

            public void InitializeReflectionMembers() {
                if (actualHashSetInstance == null) {
                    Debug.Log(GetType().Name + ": Something go wrong. Report this if you are reading this");
                    return;
                }

                Type type = actualHashSetInstance.GetType();
                dictionaryGenericTypes = type.GetGenericArguments();

                removeMethod = type.GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public, Type.DefaultBinder, new Type[1] { dictionaryGenericTypes[0] }, null);
                getIndexLookupListMethod = type.GetMethod("GetIndexLookupList", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);
                getValueLookupListMethod = type.GetMethod("GetValueLookupList", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);
                getSlotHashCodesMethod = type.GetMethod("GetSlotHashCodes", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);
            }

            public void RefreshIndexLookupList() {
                indexLookupList = (List<int>)getIndexLookupListMethod.Invoke(actualHashSetInstance, null);
                valueLookupList = (List<object>)getValueLookupListMethod.Invoke(actualHashSetInstance, null);

                hashCodeList = (List<int>)getSlotHashCodesMethod.Invoke(actualHashSetInstance, null);

                var interpreter = SerializableHashSetWindow.WindowInstance.SearchInterpreter;

                if (interpreter.IsValid) {
                    List<int> removeEntries = new List<int>();

                    for (int i = 0; i < valueLookupList.Count; i++) {
                        bool qualify = SerializableHashSetWindow.WindowInstance.SearchInterpreter.CheckQualify(valueLookupList[i], i, null);

                        if (!qualify) {
                            removeEntries.Add(i);
                        }
                    }

                    indexLookupList.RemoveAtIndices(removeEntries);
                    valueLookupList.RemoveAtIndices(removeEntries);
                    hashCodeList.RemoveAtIndices(removeEntries);
                }
            }
        }
        internal CachedReflectionProperties _cached;

        private readonly SerializedProperty _hashsetProperty;

        private readonly SerializedProperty _slotsProperty;
        private readonly SerializedProperty _countProperty;

        private int currentPage = 0;
        private int displayAmount = 10;
        public int DisplayAmountPerPage {
            get => displayAmount;

            set {
                displayAmount = Mathf.Clamp(value, SerializableHashSetWindow.DisplayAmountRange.x, SerializableHashSetWindow.DisplayAmountRange.y);
            }
        }

        public SerializableHashSetSlotDisplayer(SerializedProperty hashset, FieldInfo fieldInfo) {
            _hashsetProperty = hashset;
            _slotsProperty = _hashsetProperty.FindPropertyRelative("m_slots");
            _countProperty = _hashsetProperty.FindPropertyRelative("m_count");

            _cached = new CachedReflectionProperties() {
                actualHashSetInstance = RPEditorUtility.GetActualInstance(hashset),
            };
            _cached.InitializeReflectionMembers();
        }

        public void Initialize() {
            SerializableHashSetWindow.WindowInstance.ControlPanel.onValueAdded += () => {
                _cached.RefreshIndexLookupList();
            };
            _cached.RefreshIndexLookupList();
        }

        public void DrawDisplayLayout() {
            if (_cached.valueLookupList.Count != Count) {
                _cached.RefreshIndexLookupList();
            }

            int pageCount = Count / DisplayAmountPerPage + Math.Sign(Count % DisplayAmountPerPage / (float)DisplayAmountPerPage);

            if (pageCount != 0) {
                currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);
            } else {
                currentPage = 0;
            }

            EditorGUI.BeginChangeCheck();
            Rect rect = EditorGUILayout.BeginVertical();

            rect.y -= 2;
            rect.height += 2;

            if (Event.current.type == EventType.Repaint && Count != 0) {
                var bottomBackground = RPEditorStyleStorage.AccessStyle("BuiltIn.Dark.Background.CurveEndBackground_0");
                bottomBackground.Draw(rect, false, false, false, false);
            }

            DrawElements();

            if (removePermission == true) {
                removePermission = false;
                _cached.removeMethod.Invoke(_cached.actualHashSetInstance, new object[] { removeValue });

                removeValue = null;

                _cached.RefreshIndexLookupList();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();

            if (pageCount > 1) {
                if (currentPage != 0) {
                    if (GUILayout.Button("<--")) {
                        currentPage--;
                    }
                } else {
                    GUI.enabled = false;
                    GUILayout.Button("<--");
                    GUI.enabled = true;
                }

                DoPageDisplay();

                if (currentPage != pageCount - 1) {
                    if (GUILayout.Button("-->")) {
                        currentPage++;
                    }
                } else {
                    GUI.enabled = false;
                    GUILayout.Button("-->");
                    GUI.enabled = true;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck()) {
                _hashsetProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        void DoPageDisplay() {
            int pageCount = Count / DisplayAmountPerPage + Math.Sign(Count % DisplayAmountPerPage / (float)DisplayAmountPerPage);

            GUIContent pageDisplay = new GUIContent(currentPage + 1 + "/" + pageCount);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.alignment = TextAnchor.MiddleCenter;

            float textSize = style.CalcSize(pageDisplay).x;

            EditorGUILayout.LabelField(pageDisplay, style, GUILayout.Width(textSize));
        }

        private object removeValue;
        private bool removePermission = false; // Because HashSet can contain null value, we can't do removeValue != null

        private static readonly GUIContent valueGUIContent = new GUIContent("Value");
        private void DrawElements() {
            Vector2Int drawRange = new Vector2Int(displayAmount * currentPage, Mathf.Min(Count, displayAmount * currentPage + displayAmount));

            for (int i = drawRange.x; i < drawRange.y; i++) {
                int lookup = _cached.indexLookupList[i];
                EditorGUILayout.LabelField("Element " + i, EditorStyles.boldLabel);

                GUI.enabled = false;
                EditorGUILayout.PropertyField(_slotsProperty.GetArrayElementAtIndex(lookup).FindPropertyRelative("value"), valueGUIContent);
                GUI.enabled = true;

                if (SerializableHashSetWindow.DebugMode) {
                    GUI.enabled = false;
                    EditorGUILayout.IntField("Hash Code", _cached.hashCodeList[i]);
                    GUI.enabled = true;
                }

                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X")) {
                    removeValue = _cached.valueLookupList[i];
                    removePermission = true;
                }
                GUILayout.Space(3);

                EditorGUILayout.EndHorizontal();

                if (i != drawRange.y - 1) {
                    EditorGUILayout.Space(5);
                    var lastRect = GUILayoutUtility.GetLastRect();
                    EditorGUI.DrawRect(new Rect(lastRect.x + 4, lastRect.y + 2, lastRect.width - 8, 1), new Color(0.4f, 0.4f, 0.4f, 1));
                }
            }

            EditorGUILayout.Space(4);
        }

        public int Count => _countProperty.intValue;
    }
}