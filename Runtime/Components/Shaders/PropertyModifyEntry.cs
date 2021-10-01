using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace RealityProgrammer.UnityToolkit.Core.Rendering {
    [Serializable]
    public class PropertyModifyEntry {
        public string propertyName;

        public ShaderPropertyType propertyType;

        public float floatValue;
        public Vector4 vectorValue;
        public Color colorValue;

        public Texture textureValue;

        public int cachePropertyID;

#if UNITY_EDITOR
        public bool foldout;
        public string[] attributes;

        public Vector2 range;
        public ShaderPropertyFlags propertyFlags;
#endif
    }
}