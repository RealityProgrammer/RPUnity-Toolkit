using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    public abstract class BaseMaterialInstantiator : MonoBehaviour {
        [field: SerializeField]
        public virtual Material InstantiateMaterial { get; set; }

        public Material InstantiatedMaterial { get; protected set; }

        protected void CreateMaterial() {
            if (InstantiatedMaterial != null) {
                Destroy(InstantiatedMaterial);
            }

            if (InstantiateMaterial == null) return;

            InstantiatedMaterial = Instantiate(InstantiateMaterial);
        }

        protected virtual void OnDestroy() {
            if (InstantiatedMaterial) {
                Destroy(InstantiatedMaterial);
            }
        }
    }
}