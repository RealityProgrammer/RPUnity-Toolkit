using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RealityProgrammer.UnityToolkit.Editors.Utility {
    public static class RPEditorUtility {
        private static Func<Type, Type> _getPropertyDrawerType;

        static RPEditorUtility() {
            var internalClass = Type.GetType("UnityEditor.ScriptAttributeUtility,UnityEditor");
            var method = internalClass.GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            _getPropertyDrawerType = (Func<Type, Type>)method.CreateDelegate(typeof(Func<Type, Type>));
        }

        public static SerializedProperty FindNestedPropertyRelative(this SerializedObject serializedObject, string path, char seperator = '.') {
            string[] tokens = path.Split(seperator);

            SerializedProperty retProp = serializedObject.FindProperty(tokens[0]);

            SerializedObject so;
            for (int i = 1; i < tokens.Length; i++) {
                if (retProp == null || (retProp.propertyType == SerializedPropertyType.ObjectReference && !retProp.objectReferenceValue)) return retProp;

                if (retProp.propertyType == SerializedPropertyType.ObjectReference) {
                    so = new SerializedObject(retProp.objectReferenceValue);
                    retProp = so.FindProperty(tokens[i]);
                } else {
                    retProp = retProp.FindPropertyRelative(tokens[i]);
                }
            }

            return retProp;
        }

        public static SerializedProperty FindNestedPropertyRelative(this SerializedProperty property, string path, char seperator = '.') {
            string[] tokens = path.Split(seperator);

            SerializedObject so;
            for (int i = 0; i < tokens.Length; i++) {
                if (property == null || (property.propertyType == SerializedPropertyType.ObjectReference && !property.objectReferenceValue)) return property;

                if (property.propertyType == SerializedPropertyType.ObjectReference) {
                    so = new SerializedObject(property.objectReferenceValue);
                    property = so.FindProperty(tokens[i]);
                } else {
                    property = property.FindPropertyRelative(tokens[i]);
                }
            }

            return property;
        }

        public static object GetActualInstance(SerializedProperty property) {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;

            var elements = path.Split('.');

            for (int i = 0; i < elements.Length; i++) {
                var element = elements[i];

                if (element.Contains("[")) {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    obj = GetValue(obj, elementName, ExtractIndexFromString(element));
                } else {
                    obj = GetValue(obj, element);
                }
            }

            return obj;
        }

        public static T GetActualInstance<T>(SerializedProperty property) {
            return (T)GetActualInstance(property);
        }

        private static object GetValue(object target, string name) {
            if (target == null) return null;

            return target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static object GetValue(object target, string name, int index) {
            var enumerator = (GetValue(target, name) as IEnumerable).GetEnumerator();
            
            while (index-- >= 0) {
                enumerator.MoveNext();
            }

            return enumerator.Current;
        }

        public static int ExtractIndexFromProperty(SerializedProperty property) {
            string path = property.propertyPath;

            string nums = string.Empty;
            for (int i = path.Length - 2; i >= 0; i--) {
                if (!char.IsDigit(path[i])) break;

                nums = path[i] + nums;
            }

            return int.Parse(nums);
        }

        public static int ExtractIndexFromString(string str) {
            string nums = string.Empty;
            for (int i = str.Length - 2; i >= 0; i--) {
                if (!char.IsDigit(str[i])) break;

                nums = str[i] + nums;
            }

            return int.Parse(nums);
        }

        private static readonly HashSet<Type> builtinSerializableTypes = new HashSet<Type> {
            typeof(Vector2), typeof(Vector3), typeof(Vector4),
            typeof(Vector2Int), typeof(Vector3Int),
            typeof(Color), typeof(Color32),
            typeof(Rect), typeof(RectInt), typeof(Quaternion), typeof(Matrix4x4), typeof(LayerMask),
            typeof(AnimationCurve), typeof(Gradient), typeof(RectOffset), typeof(GUIStyle),
        };
        private static readonly Type unityEngineObjectType = typeof(UnityEngine.Object);
        private static readonly Type systemListGeneric = typeof(List<>);
        private static readonly Type typeType = typeof(Type);
        private static readonly Type stringType = typeof(string);

        public static HashSet<Type> GetBuiltInSerializableTypes() => builtinSerializableTypes;

        public static bool IsSerializableByUnity<T>() {
            return IsSerializableByUnity(typeof(T));
        }

        public static bool IsSerializableByUnity(Type type) {
            if (type.IsArray) {
                var elementType = type.GetElementType();

                return IsSerializableByUnity(elementType);
            } else if (type.IsGenericType && type.GetGenericTypeDefinition() == systemListGeneric) {
                return IsSerializableByUnity(type.GetGenericArguments()[0]);
            }

            if (type.IsAbstract || typeType == type) {
                return false;
            }

            return type.IsPrimitive || type == stringType || builtinSerializableTypes.Contains(type) || type.IsEnum || type.IsSubclassOf(unityEngineObjectType) || type == unityEngineObjectType || type.IsSerializable;
        }

        private struct AutoPropertyDrawerParameters {
            internal Rect position;
            internal GUIContent content;
            internal object input;
        }
        private delegate object AutoPropertyDrawer(AutoPropertyDrawerParameters parameters);

        private static readonly Dictionary<Type, AutoPropertyDrawer> _autoPropertyDrawer = new Dictionary<Type, AutoPropertyDrawer>() {
            [typeof(int)] = (parameters) => EditorGUI.IntField(parameters.position, parameters.content, (int)parameters.input),
            [typeof(float)] = (parameters) => EditorGUI.FloatField(parameters.position, parameters.content, (float)parameters.input),
            [typeof(long)] = (parameters) => EditorGUI.LongField(parameters.position, parameters.content, (long)parameters.input),
            [typeof(double)] = (parameters) => EditorGUI.DoubleField(parameters.position, parameters.content, (long)parameters.input),
            [typeof(string)] = (parameters) => EditorGUI.TextField(parameters.position, parameters.content, (string)parameters.input),
            [typeof(char)] = (parameters) => {
                var t = EditorGUI.TextField(parameters.position, parameters.content, ((char)parameters.input).ToString());
                if (string.IsNullOrEmpty(t)) return '\0';

                return t[0];
            },
            [typeof(uint)] = (parameters) => (uint)EditorGUI.LongField(parameters.position, parameters.content, (uint)parameters.input),
            [typeof(byte)] = (parameters) => (byte)EditorGUI.IntField(parameters.position, parameters.content, (byte)parameters.input),
            [typeof(sbyte)] = (parameters) => (sbyte)EditorGUI.IntField(parameters.position, parameters.content, (sbyte)parameters.input),
            [typeof(bool)] = (parameters) => EditorGUI.Toggle(parameters.position, parameters.content, (bool)parameters.input),
            [typeof(short)] = (parameters) => (short)EditorGUI.IntField(parameters.position, parameters.content, (short)parameters.input),
            [typeof(ushort)] = (parameters) => (ushort)EditorGUI.IntField(parameters.position, parameters.content, (ushort)parameters.input),

            [typeof(Vector2)] = (parameters) => EditorGUI.Vector2Field(parameters.position, parameters.content, (Vector2)parameters.input),
            [typeof(Vector2Int)] = (parameters) => EditorGUI.Vector2IntField(parameters.position, parameters.content, (Vector2Int)parameters.input),
            [typeof(Vector3)] = (parameters) => EditorGUI.Vector3Field(parameters.position, parameters.content, (Vector3)parameters.input),
            [typeof(Vector3Int)] = (parameters) => EditorGUI.Vector3IntField(parameters.position, parameters.content, (Vector3Int)parameters.input),
            [typeof(Vector4)] = (parameters) => EditorGUI.Vector4Field(parameters.position, parameters.content, (Vector4)parameters.input),
            [typeof(Quaternion)] = (parameters) => {
                Quaternion q = (Quaternion)parameters.input;
                Vector4 v4 = EditorGUI.Vector4Field(parameters.position, parameters.content, new Vector4(q.x, q.y, q.z, q.w));

                return new Quaternion(v4.x, v4.y, v4.z, v4.w);
            },
            [typeof(Rect)] = (parameters) => EditorGUI.RectField(parameters.position, parameters.content, (Rect)parameters.input),
            [typeof(RectInt)] = (parameters) => EditorGUI.RectIntField(parameters.position, parameters.content, (RectInt)parameters.input),
            [typeof(Bounds)] = (parameters) => EditorGUI.BoundsField(parameters.position, parameters.content, (Bounds)parameters.input),
            [typeof(BoundsInt)] = (parameters) => EditorGUI.BoundsIntField(parameters.position, parameters.content, (BoundsInt)parameters.input),
            [typeof(Color)] = (parameters) => EditorGUI.ColorField(parameters.position, parameters.content, (Color)parameters.input),
            [typeof(Color32)] = (parameters) => EditorGUI.ColorField(parameters.position, parameters.content, (Color32)parameters.input),
            [typeof(LayerMask)] = (parameters) => EditorGUI.LayerField(parameters.position, parameters.content, (LayerMask)parameters.input),
            [typeof(AnimationCurve)] = (parameters) => EditorGUI.CurveField(parameters.position, parameters.content, (AnimationCurve)parameters.input),
            [typeof(Gradient)] = (parameters) => EditorGUI.GradientField(parameters.position, parameters.content, (Gradient)parameters.input),
        };

        internal static bool AutoPropertyField(Rect rect, GUIContent content, object input, out object output) {
            if (input == null) {
                output = null;
                return false;
            }

            if (_autoPropertyDrawer.TryGetValue(input.GetType(), out var drawer)) {
                var parameters = new AutoPropertyDrawerParameters() {
                    position = rect, content = content, input = input,
                };

                output = drawer(parameters);
                return true;
            }

            output = null;
            return false;
        }
    }
}