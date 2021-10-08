using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RealityProgrammer.UnityToolkit.Editors.Utility {
    public static class RPEditorUtility {
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
    }
}