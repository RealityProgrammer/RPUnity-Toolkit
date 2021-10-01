using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Rendering {
    [DisallowMultipleComponent]
    public class SubspriteRectCalculator : MonoBehaviour {
        public string propertyName;

        MaterialPropertyBlock matBlock;

        int propertyID;

        [SerializeField] SpriteRenderer targetRenderer;

        private void Start() {
            propertyID = Shader.PropertyToID(propertyName);
            matBlock = new MaterialPropertyBlock();
        }

        private void Update() {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null || targetRenderer.sprite == null || propertyID < 0) return;

            targetRenderer.GetPropertyBlock(matBlock);

            matBlock.SetVector(propertyID, CalculateRect(targetRenderer.sprite));

            targetRenderer.SetPropertyBlock(matBlock);
        }

        Vector4 CalculateRect(Sprite sprite) {
            return new Vector4(sprite.textureRect.min.x / sprite.texture.width,
                sprite.textureRect.min.y / sprite.texture.height,
                sprite.textureRect.max.x / sprite.texture.width,
                sprite.textureRect.max.y / sprite.texture.height);
        }
    }
}