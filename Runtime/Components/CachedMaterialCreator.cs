using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using RealityProgrammer.UnityToolkit.Core.Rendering;
using RealityProgrammer.UnityToolkit.Core.Utility;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    public class CachedMaterialCreator : MonoBehaviour {
        [SerializeField] string materialKey;
        [SerializeField] Material originalMaterial;

        [SerializeField] bool overrideIfExists;

        [SerializeField] CallMethod callMethod = CallMethod.Awake;

        public PropertyModifyEntry[] propertyEntries;

        [SerializeField] bool localSceneOnly;

        private void Awake() {
            if (callMethod != CallMethod.Awake) return;

            Create();
        }
        private void Start() {
            if (callMethod != CallMethod.Start) return;

            Create();
        }

        private void Create() {
            string key = materialKey.Trim();

            if (key == string.Empty) {
                Debug.LogError("CachedMaterialCreator: Cannot create cached material with empty material key.");
                return;
            }

            if (CachedMaterialStorage.HasCachedMaterial(key) && overrideIfExists) {
                CachedMaterialStorage.AssignCachedMaterial(key, InstantiateMaterial());

                if (localSceneOnly) {
                    SceneManager.sceneUnloaded += OnSceneUnloaded;
                }
            } else {
                CachedMaterialStorage.AssignCachedMaterial(key, InstantiateMaterial());

                if (localSceneOnly) {
                    SceneManager.sceneUnloaded += OnSceneUnloaded;
                }
            }
        }

        private Material InstantiateMaterial() {
            if (originalMaterial == null) {
                Debug.LogError("CachedMaterialCreator: Cannot instantiate material as original material is null.");
                return null;
            }

            var instantiated = Instantiate(originalMaterial);

            for (int i = 0; i < propertyEntries.Length; i++) {
                var entry = propertyEntries[i];

                entry.cachePropertyID = Shader.PropertyToID(entry.propertyName);

                switch (entry.propertyType) {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        instantiated.SetFloat(entry.cachePropertyID, entry.floatValue);
                        break;

                    case ShaderPropertyType.Color:
                        instantiated.SetColor(entry.cachePropertyID, entry.colorValue);
                        break;

                    case ShaderPropertyType.Vector:
                        instantiated.SetVector(entry.cachePropertyID, entry.vectorValue);
                        break;

                    case ShaderPropertyType.Texture:
                        instantiated.SetTexture(entry.cachePropertyID, entry.textureValue);
                        break;
                }
            }

            return instantiated;
        }

        void OnSceneUnloaded(Scene scene) {
            if (scene.name == scene.name) {
                CachedMaterialStorage.DestroyCachedMaterial(materialKey.Trim());
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }
        }

        public enum CallMethod {
            Awake, Start
        }
    }
}