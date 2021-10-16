using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RealityProgrammer.CSStandard.Interpreter;

namespace RealityProgrammer.UnityToolkit.Editors.Windows.SerializableHashSet {
    internal class SerializableHashSetWindow : EditorWindow {
        public static readonly Vector2Int DisplayAmountRange = new Vector2Int(5, 15);

        public static SerializableHashSetWindow WindowInstance { get; protected set; }

        public SerializedProperty HashSetProperty { get; protected set; }

        public ConditionalSearchInterpreter SearchInterpreter { get; protected set; }
        public SerializableHashSetControlPanel ControlPanel { get; protected set; }
        public SerializableHashSetSlotDisplayer Displayer { get; protected set; }

        public static void InitializeWindow(SerializedProperty original, FieldInfo fieldInfo) {
            WindowInstance = GetWindow<SerializableHashSetWindow>();

            WindowInstance.titleContent = new GUIContent("Serialzable HashSet Window");
            WindowInstance.minSize = new Vector2(350, 600);

            WindowInstance.Initialize(original, fieldInfo);
        }

        protected virtual void Initialize(SerializedProperty original, FieldInfo field) {
            HashSetProperty = original;

            ControlPanel = new SerializableHashSetControlPanel(original, field);
            Displayer = new SerializableHashSetSlotDisplayer(original, field);
            SearchInterpreter = new ConditionalSearchInterpreter();

            wantsMouseMove = true;

            //ControlPanel.Initialize();
            Displayer.Initialize();
        }

        Vector2 scrollViewPosition;
        protected virtual void OnGUI() {
            if (HashSetProperty == null) {
                Close();
                return;
            }

            if (HashSetProperty.serializedObject == null) {
                Close();
                return;
            }

            HashSetProperty.serializedObject.Update();

            scrollViewPosition = EditorGUILayout.BeginScrollView(scrollViewPosition);

            ControlPanel.DrawLayout();
            Displayer.DrawDisplayLayout();

            EditorGUILayout.EndScrollView();

            HashSetProperty.serializedObject.ApplyModifiedProperties();
        }

        internal static readonly string DebugModeKey = "RealityProgrammer.UnityToolkit.Windows.SerializableHashSet.Debug";
        public static bool DebugMode {
            get {
                if (EditorPrefs.HasKey(DebugModeKey)) {
                    return EditorPrefs.GetBool(DebugModeKey);
                }

                EditorPrefs.SetBool(DebugModeKey, false);
                return false;
            }

            set {
                EditorPrefs.SetBool(DebugModeKey, value);
            }
        }
    }
}