using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RealityProgrammer.UnityToolkit.Editors.Utility {
    public static class RPEditorStyleStorage {
        internal const string DarkBuiltInResourcePath = "Dark/Built-In/";

        private static readonly Dictionary<string, GUIStyle> _storage;

        static RPEditorStyleStorage() {
            _storage = new Dictionary<string, GUIStyle>();

            var variable = new GUIStyle() {
                border = new RectOffset(1, 1, 14, 14),
            };

            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "DarkSmoothCylinder_Bg_0");

            _storage.Add("BuiltIn.Dark.Background.DarkSmoothCylinder_0", variable);

            variable = new GUIStyle() {
                border = new RectOffset(1, 1, 10, 10),
            };

            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "DarkSmoothCylinder_Bg_1");

            _storage.Add("BuiltIn.Dark.Background.DarkSmoothCylinder_1", variable);

            GUIStyle button0 = new GUIStyle() {
                border = new RectOffset(1, 1, 10, 10),
                padding = new RectOffset(3, 3, 0, 0),
                alignment = TextAnchor.MiddleLeft,
            };

            button0.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            button0.normal.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "Button0_Normal");
            button0.hover.textColor = RPEditorUIUtility.GetDefaultTextColor();
            button0.hover.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "Button0_Hover");
            button0.active.textColor = RPEditorUIUtility.GetDefaultTextColor();
            button0.active.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "Button0_Hold");
            button0.fontStyle = FontStyle.Bold;

            RegisterStyle("BuiltIn.Dark.Button.DarkGradient_0", button0);

            variable = new GUIStyle() {
                border = new RectOffset(2, 2, 2, 2),
                fixedWidth = 10,
                fixedHeight = 10,
            };

            variable.normal.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "Searchbar_NormalCancelButton");
            variable.hover.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "Searchbar_HoverCancelButton");

            RegisterStyle("BuiltIn.Dark.Button.SearchbarCancel", variable);

            variable = new GUIStyle {
                border = new RectOffset(1, 1, 0, 1),
            };
            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "Background_BottomConnect0");

            RegisterStyle("BuiltIn.Dark.Background.BottomConnect_0", variable);

            variable = new GUIStyle {
                border = new RectOffset(4, 4, 0, 4),
            };
            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>(DarkBuiltInResourcePath + "CurveEndBackground_0");

            RegisterStyle("BuiltIn.Dark.Background.CurveEndBackground_0", variable);
        }

        public static GUIStyle AccessStyle(string key) {
            if (_storage.TryGetValue(key, out var output)) {
                return output;
            }

            return null;
        }

        public static GUIStyle CopyStyle(string key) {
            return new GUIStyle(AccessStyle(key));
        }

        public static bool TryCopyStyle(string key, out GUIStyle clone) {
            if (_storage.TryGetValue(key, out var output)) {
                clone = new GUIStyle(output);
                return true;
            }

            clone = null;
            return false;
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