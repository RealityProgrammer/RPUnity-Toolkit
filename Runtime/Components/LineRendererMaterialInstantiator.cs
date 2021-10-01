using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    public sealed class LineRendererMaterialInstantiator : BaseMaterialInstantiator {
        [SerializeField] LineRenderer target;

        private void Awake() {
            CreateMaterial();

            target.material = InstantiatedMaterial;
        }

        public LineRenderer GetLineRenderer() {
            return target;
        }
    }
}