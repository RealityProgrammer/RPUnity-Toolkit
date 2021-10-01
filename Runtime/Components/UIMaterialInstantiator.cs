using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    [DisallowMultipleComponent]
    public class UIMaterialInstantiator : BaseMaterialInstantiator {
        [SerializeField] Graphic targetGraphic;

        private void Awake() {
            CreateMaterial();

            targetGraphic.material = InstantiatedMaterial;
        }

        public Graphic RetrieveGraphic() {
            return targetGraphic;
        }
    }
}