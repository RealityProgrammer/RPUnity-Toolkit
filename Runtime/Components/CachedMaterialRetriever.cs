using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityProgrammer.UnityToolkit.Core.Utility;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    [DisallowMultipleComponent]
    public class CachedMaterialRetriever : MonoBehaviour {
        private Renderer myRenderer;

        [SerializeField] string materialKey;
        [SerializeField, Tooltip("Determine whether to throw error on null Material")] bool throwErrorNullMat = true;

        private void Start() {
            myRenderer = GetComponent<Renderer>();

            if (myRenderer == null) {
                Debug.LogError("CachedMaterialRetriever: Cannot grab the Renderer component of current GameObject");
                return;
            }

            string key = materialKey.Trim();

            if (key == string.Empty) {
                Debug.LogError("CachedMaterialRetriever: Cannot retrieve cached material with empty material key.");
                return;
            }

            Material material = CachedMaterialStorage.RetrieveCachedMaterial(key);

            if (material == null && throwErrorNullMat) {
                Debug.LogError($"CachedMaterialRetriever: Retrieved cached material is null as the key [{key}] is invalid.");
                return;
            }

            myRenderer.material = material;
        }
    }
}