using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using RealityProgrammer.UnityToolkit.Core.Components;

namespace RealityProgrammer.UnityToolkit.Core.Rendering {
    [DisallowMultipleComponent]
    public sealed class UIShaderPropertyController : MonoBehaviour {
        [SerializeField] UIMaterialInstantiator target;

        public PropertyModifyEntry[] entries;

        void Start() {
            if (entries != null && entries.Length != 0 && target != null) {
                var mat = target.InstantiatedMaterial;

                for (int i = 0; i < entries.Length; i++) {
                    var entry = entries[i];

                    entry.cachePropertyID = Shader.PropertyToID(entry.propertyName);

                    switch (entry.propertyType) {
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:
                            mat.SetFloat(entry.cachePropertyID, entry.floatValue);
                            break;

                        case ShaderPropertyType.Color:
                            mat.SetColor(entry.cachePropertyID, entry.colorValue);
                            break;

                        case ShaderPropertyType.Vector:
                            mat.SetVector(entry.cachePropertyID, entry.vectorValue);
                            break;

                        case ShaderPropertyType.Texture:
                            mat.SetTexture(entry.cachePropertyID, entry.textureValue);
                            break;
                    }
                }
            }
        }
    }
}