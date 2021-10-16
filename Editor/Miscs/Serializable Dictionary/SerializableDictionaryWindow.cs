using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using RealityProgrammer.CSStandard.Interpreter;

namespace RealityProgrammer.UnityToolkit.Editors.Windows.SerializableDictionary {
    internal class SerializableDictionaryWindow : EditorWindow {
        public static readonly Vector2Int DisplayAmountRange = new Vector2Int(5, 15);

        public static SerializableDictionaryWindow WindowInstance { get; private set; }

        public SerializedProperty DictionaryProperty { get; protected set; }

        public ConditionalSearchInterpreter SearchInterpreter { get; protected set; }
        public SerializableDictionaryControlPanel ControlPanel { get; protected set; }
        public SerializableDictionaryPairDisplayer Displayer { get; protected set; }

        public static void InitializeWindow(SerializedProperty original, FieldInfo fieldInfo) {
            WindowInstance = GetWindow<SerializableDictionaryWindow>();

            WindowInstance.titleContent = new GUIContent("Serialzable Dictionary Window");
            WindowInstance.minSize = new Vector2(350, 600);

            WindowInstance.Initialize(original, fieldInfo);
        }

        protected virtual void Initialize(SerializedProperty original, FieldInfo field) {
            DictionaryProperty = original;

            ControlPanel = new SerializableDictionaryControlPanel(original, field);
            Displayer = new SerializableDictionaryPairDisplayer(original, field);
            SearchInterpreter = new ConditionalSearchInterpreter();

            wantsMouseMove = true;

            Displayer.Initialize();
        }

        Vector2 scrollViewPosition;
        protected virtual void OnGUI() {
            if (DictionaryProperty == null) {
                Close();
                return;
            }

            if (DictionaryProperty.serializedObject == null) {
                Close();
                return;
            }

            DictionaryProperty.serializedObject.Update();

            scrollViewPosition = EditorGUILayout.BeginScrollView(scrollViewPosition);

            ControlPanel.DrawLayout();
            Displayer.DrawDisplayLayout();

            EditorGUILayout.EndScrollView();

            DictionaryProperty.serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnDestroy() {
            ControlPanel.CloseWindow();
        }
    }
}