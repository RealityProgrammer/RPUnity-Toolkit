using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace RealityProgrammer.UnityToolkit.Core.Rendering {
    [DisallowMultipleComponent]
    public class ShaderPropertyController : MonoBehaviour {
        [SerializeField] Renderer targetRenderer;
        public PropertyModifyEntry[] entries;

        MaterialPropertyBlock materialBlock;

        private void Start() {
            materialBlock = new MaterialPropertyBlock();

            if (entries != null && targetRenderer != null) {
                for (int i = 0; i < entries.Length; i++) {
                    entries[i].cachePropertyID = Shader.PropertyToID(entries[i].propertyName);
                }
            }
        }

        private void Update() {
            if (entries == null || entries.Length == 0 || targetRenderer == null) return;

            targetRenderer.GetPropertyBlock(materialBlock);

            for (int i = 0; i < entries.Length; i++) {
                var entry = entries[i];

                switch (entry.propertyType) {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        materialBlock.SetFloat(entry.cachePropertyID, entry.floatValue);
                        break;

                    case ShaderPropertyType.Color:
                        materialBlock.SetColor(entry.cachePropertyID, entry.colorValue);
                        break;

                    case ShaderPropertyType.Vector:
                        materialBlock.SetVector(entry.cachePropertyID, entry.vectorValue);
                        break;

                    case ShaderPropertyType.Texture:
                        materialBlock.SetTexture(entry.cachePropertyID, entry.textureValue);
                        break;
                }
            }

            targetRenderer.SetPropertyBlock(materialBlock);
        }

        public PropertyModifyEntry RetrieveProperty(string name) {
            for (int i = 0; i < entries.Length; i++) {
                if (entries[i].propertyName == name) return entries[i];
            }

            return null;
        }

        public Renderer AccessRenderer() => targetRenderer;
    }
}