using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using RealityProgrammer.UnityToolkit.Editors.Miscs;
using RealityProgrammer.CSStandard.Interpreter;
using RealityProgrammer.UnityToolkit.Editors.Utility;

namespace RealityProgrammer.UnityToolkit.Editors.Windows {
    public sealed class SerializableDictionaryWindow : EditorWindow {
        public static readonly Vector2Int DisplayAmountRange = new Vector2Int(5, 15);

        public static SerializableDictionaryWindow WindowInstance { get; private set; }

        public SerializedProperty DictionaryProperty { get; private set; }

        public ConditionalSearchInterpreter SearchInterpreter { get; private set; }
        public SerializableDictionaryControlPanel ControlPanel { get; private set; }
        public SerializableDictionaryPairDisplayer Displayer { get; private set; }

        public static void InitializeWindow(SerializedProperty original, FieldInfo fieldInfo) {
            WindowInstance = GetWindow<SerializableDictionaryWindow>();

            WindowInstance.titleContent = new GUIContent("Serialzable Dictionary Window");
            WindowInstance.minSize = new Vector2(350, 600);

            WindowInstance.Initialize(original, fieldInfo);
        }

        private void Initialize(SerializedProperty original, FieldInfo field) {
            DictionaryProperty = original;

            ControlPanel = new SerializableDictionaryControlPanel(original, field);
            Displayer = new SerializableDictionaryPairDisplayer(original, field);
            SearchInterpreter = new ConditionalSearchInterpreter();

            ControlPanel.Initialize();
            Displayer.Initialize();
        }

        Vector2 scrollViewPosition;
        private void OnGUI() {
            if (DictionaryProperty == null) {
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

        private void Update() {
            Repaint();
        }
    }
}