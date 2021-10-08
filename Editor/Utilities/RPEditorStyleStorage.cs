using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RealityProgrammer.UnityToolkit.Editors.Utility {
    public static class RPEditorStyleStorage {
        private static readonly Dictionary<string, GUIStyle> _storage;

        static RPEditorStyleStorage() {
            _storage = new Dictionary<string, GUIStyle>();

            var variable = new GUIStyle() {
                border = new RectOffset(1, 1, 14, 14),
            };

            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>("Dark/DarkSmoothCylinder_Bg_0");

            _storage.Add("Background.DarkSmoothCylinder_0", variable);

            variable = new GUIStyle() {
                border = new RectOffset(1, 1, 10, 10),
            };

            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>("Dark/DarkSmoothCylinder_Bg_1");

            _storage.Add("Background.DarkSmoothCylinder_1", variable);
        }

        public static GUIStyle AccessStyle(string key) {
            if (_storage.TryGetValue(key, out var output)) {
                return output;
            }

            return null;
        }

        public static bool RegisterStyle(string key, GUIStyle style) {
            if (style != null && !_storage.ContainsKey(key)) {
                _storage.Add(key, style);
                return true;
            }

            return false;
        }

        public static bool TryAccessStyle(string key, out GUIStyle output) {
            if (_storage.TryGetValue(key, out output)) {
                return true;
            }

            output = null;
            return false;
        }

        public static bool ContainsStyle(string key) {
            return _storage.ContainsKey(key);
        }
    }
}