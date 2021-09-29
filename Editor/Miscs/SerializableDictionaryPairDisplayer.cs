using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.UnityToolkit.Editors.Utility;
using RealityProgrammer.UnityToolkit.Editors.Windows;

namespace RealityProgrammer.UnityToolkit.Editors.Miscs {
    public class SerializableDictionaryPairDisplayer {
        public class CachedReflectionProperties {
            private SerializableDictionaryPairDisplayer owner;

            public CachedReflectionProperties(SerializableDictionaryPairDisplayer owner) {
                this.owner = owner;
            }

            public IDictionary actualDictionaryInstance;

            public FieldInfo fieldInfo;

            public Type[] dictionaryGenericTypes;

            public MethodInfo removeKeyMethod, getIndexLookupListMethod, getKeyLookupArrayMethod;

            public List<int> indexLookupList;
            public List<object> keyLookupArray;

            public void InitializeReflectionMembers() {
                if (actualDictionaryInstance == null) {
                    Debug.Log(GetType().Name + ": Something go wrong. Report this if you are reading this");
                    return;
                }

                Type type = actualDictionaryInstance.GetType();
                dictionaryGenericTypes = type.GetGenericArguments();

                removeKeyMethod = type.GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public, Type.DefaultBinder, new Type[1] { dictionaryGenericTypes[0] }, null);
                getIndexLookupListMethod = type.GetMethod("GetIndexLookupList", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);
                getKeyLookupArrayMethod = type.GetMethod("GetKeyLookupArray", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, Type.EmptyTypes, null);
            }

            public void RefreshIndexLookupList() {
                indexLookupList = (List<int>)getIndexLookupListMethod.Invoke(actualDictionaryInstance, null);
                keyLookupArray = (List<object>)getKeyLookupArrayMethod.Invoke(actualDictionaryInstance, null);

                var interpreter = SerializableDictionaryWindow.WindowInstance.SearchInterpreter;

                if (interpreter.IsValid) {
                    List<int> removeEntries = new List<int>();

                    SerializableDictionaryWindow.WindowInstance.SearchInterpreter.InitializeProgram("__iterator__ >= 3");

                    for (int i = 0; i < keyLookupArray.Count; i++) {
                        bool qualify = SerializableDictionaryWindow.WindowInstance.SearchInterpreter.CheckQualify(keyLookupArray[i], i, null);

                        if (!qualify) {
                            removeEntries.Add(i);
                        }
                    }

                    if (removeEntries.Count != 0) {
                        int indexToRemove = 0;
                        int newIdx = 0;

                        for (int originalIdx = 0; originalIdx < indexLookupList.Count; originalIdx++) {
                            if (indexToRemove < removeEntries.Count && removeEntries[indexToRemove] == originalIdx) {
                                indexToRemove++;
                            } else {
                                int increment = newIdx++;

                                indexLookupList[increment] = indexLookupList[originalIdx];
                                keyLookupArray[increment] = keyLookupArray[originalIdx];
                            }
                        }

                        indexLookupList.RemoveRange(newIdx, indexLookupList.Count - newIdx);
                        keyLookupArray.RemoveRange(newIdx, keyLookupArray.Count - newIdx);
                    }
                }
            }
        }
        internal CachedReflectionProperties _cached;

        private readonly SerializedProperty _dictionaryProperty;

        private readonly SerializedProperty _entriesProperty;
        private readonly SerializedProperty _countProperty, _freeCountProperty;

        private int currentPage = 0;
        private int displayAmount = 10;
        public int DisplayAmountPerPage {
            get => displayAmount;

            set {
                displayAmount = Mathf.Clamp(value, SerializableDictionaryWindow.DisplayAmountRange.x, SerializableDictionaryWindow.DisplayAmountRange.y);
            }
        }

        public SerializableDictionaryPairDisplayer(SerializedProperty dictionary, FieldInfo fieldInfo) {
            _dictionaryProperty = dictionary;
            _entriesProperty = _dictionaryProperty.FindPropertyRelative("entries");
            _countProperty = _dictionaryProperty.FindPropertyRelative("count");
            _freeCountProperty = _dictionaryProperty.FindPropertyRelative("freeCount");

            _cached = new CachedReflectionProperties(this) {
                actualDictionaryInstance = RPEditorUtility.GetActualInstance(fieldInfo, dictionary) as IDictionary,
            };
            _cached.InitializeReflectionMembers();
        }

        public void Initialize() {
            SerializableDictionaryWindow.WindowInstance.ControlPanel.onPairAdd += () => {
                _cached.RefreshIndexLookupList();
            };
            _cached.RefreshIndexLookupList();
        }

        private static readonly GUIContent keyGUIContent = new GUIContent("Key");
        private static readonly GUIContent valueGUIContent = new GUIContent("Value");

        private object removeKey; // Prevent NRE

        public void DrawDisplayLayout() {
            _dictionaryProperty.serializedObject.Update();

            //int total = Count;
            int total = _cached.keyLookupArray.Count;
            int pageCount = total / DisplayAmountPerPage + Math.Sign(total % DisplayAmountPerPage / (float)DisplayAmountPerPage);

            if (pageCount != 0) {
                currentPage = Mathf.Clamp(currentPage, 0, pageCount - 1);
            } else {
                currentPage = 0;
            }

            EditorGUI.BeginChangeCheck();
            Rect rect = EditorGUILayout.BeginVertical();

            rect.y -= 2;
            rect.height += 2;

            if (Event.current.type == EventType.Repaint && total != 0) {
                var bottomBackground = RPEditorUIStorage.AccessStyle("SerializableDictionary.PairDisplayer.NormalBackground.Dark");
                bottomBackground.Draw(rect, false, false, false, false);
            }

            Vector2Int drawRange = new Vector2Int(displayAmount * currentPage, Mathf.Min(total, displayAmount * currentPage + displayAmount));

            for (int i = drawRange.x; i < drawRange.y; i++) {
                EditorGUILayout.LabelField("Element " + i, EditorStyles.boldLabel);
                int lookup = _cached.indexLookupList[i];

                GUI.enabled = false;
                EditorGUILayout.PropertyField(_entriesProperty.GetArrayElementAtIndex(lookup).FindPropertyRelative("key"), keyGUIContent);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(_entriesProperty.GetArrayElementAtIndex(lookup).FindPropertyRelative("value"), valueGUIContent);

                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X")) {
                    removeKey = _cached.keyLookupArray[i];
                }
                GUILayout.Space(3);

                EditorGUILayout.EndHorizontal();

                if (i != drawRange.y - 1) {
                    EditorGUILayout.Space(5);
                    var lastRect = GUILayoutUtility.GetLastRect();
                    EditorGUI.DrawRect(new Rect(lastRect.x + 4, lastRect.y + 2, lastRect.width - 8, 1), new Color(0.4f, 0.4f, 0.4f, 1));
                }
            }

            EditorGUILayout.Space(5);
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

            if (removeKey != null) {
                _cached.removeKeyMethod.Invoke(_cached.actualDictionaryInstance, new object[1] { removeKey });

                removeKey = null;
                _cached.RefreshIndexLookupList();
            }

            if (EditorGUI.EndChangeCheck()) {
                _dictionaryProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        void DoPageDisplay() {
            int total = _cached.keyLookupArray.Count;
            int pageCount = total / DisplayAmountPerPage + Math.Sign(total % DisplayAmountPerPage / (float)DisplayAmountPerPage);

            GUIContent pageDisplay = new GUIContent(currentPage + 1 + "/" + pageCount);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.alignment = TextAnchor.MiddleCenter;

            float textSize = style.CalcSize(pageDisplay).x;

            EditorGUILayout.LabelField(pageDisplay, style, GUILayout.Width(textSize));
        }

        private int Count => _countProperty.intValue - _freeCountProperty.intValue;
    }
}