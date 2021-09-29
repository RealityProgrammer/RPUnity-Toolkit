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

        public static object GetActualInstance(FieldInfo fieldInfo, SerializedProperty property) {
            var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (obj == null) { return null; }

            object actualObject;
            Type objectType = obj.GetType();

            if (objectType.IsArray) {
                actualObject = ((object[])obj)[ExtractIndex(property)];
            } else if (obj is IList ilist && objectType.IsGenericType) {
                actualObject = ilist[ExtractIndex(property)];
            } else {
                actualObject = obj;
            }

            return actualObject;
        }

        public static T GetActualInstance<T>(FieldInfo fieldInfo, SerializedProperty property) {
            var obj = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (obj == null) { return default; }

            object actualObject;
            Type objectType = obj.GetType();

            if (objectType.IsArray) {
                actualObject = ((object[])obj)[ExtractIndex(property)];
            } else if (obj is IList ilist && objectType.IsGenericType) {
                actualObject = ilist[ExtractIndex(property)];
            } else {
                actualObject = obj;
            }

            return (T)actualObject;
        }

        public static int ExtractIndex(SerializedProperty property) {
            string path = property.propertyPath;

            string nums = string.Empty;
            for (int i = path.Length - 2; i >= 0; i--) {
                if (!char.IsDigit(path[i])) break;

                nums = path[i] + nums;
            }

            return int.Parse(nums);
        }

        private static readonly Type[] builtinSerializableTypes = new Type[] {
            typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Color),
            typeof(Vector2Int), typeof(Vector3Int), /* This is sad typeof(Vector4Int), */ typeof(Color32),
            typeof(Rect), typeof(RectInt), typeof(Quaternion), typeof(Matrix4x4), typeof(LayerMask),
            typeof(AnimationCurve), typeof(Gradient), typeof(RectOffset), typeof(GUIStyle),
        };
        private static readonly Type unityEngineObjectType = typeof(UnityEngine.Object);

        public static bool IsSerializableByUnity<T>() {
            return IsSerializableByUnity(typeof(T));
        }

        public static bool IsSerializableByUnity(Type type) {
            if (type.IsArray) {
                var elementType = type.GetElementType();

                return IsSerializableByUnity(elementType);
            }

            return builtinSerializableTypes.Contains(type) || type.IsSubclassOf(unityEngineObjectType) || type == unityEngineObjectType || type.IsSerializable;
        }
    }
}