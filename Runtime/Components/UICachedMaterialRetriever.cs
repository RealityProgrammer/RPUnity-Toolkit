using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RealityProgrammer.UnityToolkit.Core.Utility;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    [DisallowMultipleComponent]
    public class UICachedMaterialRetriever : MonoBehaviour {
        private Graphic myGraphic;

        [SerializeField] string materialKey;
        [SerializeField, Tooltip("Determine whether to throw error on null Material")] bool throwErrorNullMat = true;

        private void Start() {
            myGraphic = GetComponent<Graphic>();

            if (myGraphic == null) {
                Debug.LogError("UICachedMaterialRetriever: Cannot grab the Graphic component of current GameObject");
                return;
            }

            string key = materialKey.Trim();

            if (key == string.Empty) {
                Debug.LogError("UICachedMaterialRetriever: Cannot retrieve cached material with empty material key.");
                return;
            }

            Material material = CachedMaterialStorage.RetrieveCachedMaterial(key);

            if (material == null && throwErrorNullMat) {
                Debug.LogError($"UICachedMaterialRetriever: Retrieved cached material is null as the key [{key}] is invalid.");
                return;
            }

            myGraphic.material = material;
        }
    }
}