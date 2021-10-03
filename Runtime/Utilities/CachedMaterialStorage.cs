using System.Collections.Generic;
using UnityEngine;

namespace RealityProgrammer.UnityToolkit.Core.Utility {
    public static class CachedMaterialStorage {
        private static readonly Dictionary<string, Material> _cachedMats = new Dictionary<string, Material>();

        // Cache Material Stuff
        public static Material CreateCachedMaterial(string id, Material originalMaterial) {
            var instantiate = Object.Instantiate(originalMaterial);
            _cachedMats[id] = instantiate;

            return instantiate;
        }

        public static void AssignCachedMaterial(string id, Material clone) {
            _cachedMats[id] = clone;
        }

        public static bool HasCachedMaterial(string id) {
            return _cachedMats.ContainsKey(id);
        }

        public static Material RetrieveCachedMaterial(string id) {
            try {
                return _cachedMats[id];
            } catch (KeyNotFoundException) {
                Debug.LogError("CachedMaterialStorage: Cannot find key to retrieve cached material: " + id);
                return null;
            }
        }

        public static bool TryRetrieveCachedMaterial(string id, out Material mat) {
            return _cachedMats.TryGetValue(id, out mat);
        }

        public static int CountCachedMaterials() {
            return _cachedMats.Count;
        }

        public static bool DestroyCachedMaterial(string id) {
            if (_cachedMats.ContainsKey(id)) {
                Object.Destroy(_cachedMats[id]);
                _cachedMats.Remove(id);

                return true;
            }

            return false;
        }

        public static bool RemoveCachedMaterial(string id) {
            return _cachedMats.Remove(id);
        }

        public static void DestroyAllCachedMaterials() {
#if RP_UNITYTOOLKIT_RUNTIME_DEBUG
            int i = 0;

            foreach (var pair in _cachedMats) {
                if (pair.Value != null) {
                    Object.Destroy(pair.Value);
                    i++;
                }
            }

            Debug.Log("CachedMaterialStorage: Destroyed " + i + " cached materials");
#else
            foreach (var pair in _cachedMats) {
                if (pair.Value != null) {
                    Object.Destroy(pair.Value);
                }
            }
#endif
            _cachedMats.Clear();
        }
    }
}