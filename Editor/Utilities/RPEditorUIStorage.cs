using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RealityProgrammer.UnityToolkit.Editors.Utility {
    public static class RPEditorUIStorage {
        private static readonly Dictionary<string, GUIStyle> _storage;

        static RPEditorUIStorage() {
            _storage = new Dictionary<string, GUIStyle>();

            GUIStyle variable = new GUIStyle() {
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(3, 3, 1, 1),
            };

            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>("Dark/SDCP_NormalBgHeader");
            _storage.Add("SerializableDictionary.ControlPanel.BackgroundHeader.Dark", variable);

            variable = new GUIStyle(_storage["SerializableDictionary.ControlPanel.BackgroundHeader.Dark"]);
            variable.padding.top = 4;
            variable.padding.bottom = 4;
            variable.hover.background = Resources.Load<Texture2D>("Dark/SDCP_HoverBgHeader");
            variable.hover.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.active.background = Resources.Load<Texture2D>("Dark/SDCP_HoldBgHeader");
            variable.active.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.fontStyle = FontStyle.Bold;

            _storage.Add("SerializableDictionary.ControlPanel.BackgroundHeader.Dark.Button0", variable);

            variable = new GUIStyle {
                border = new RectOffset(1, 1, 0, 1),
            };
            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>("Dark/SDCP_NormalBgBottom");

            _storage.Add("SerializableDictionary.ControlPanel.BackgroundBottom.Dark", variable);

            variable = new GUIStyle {
                border = new RectOffset(4, 4, 0, 4),
            };
            variable.normal.textColor = RPEditorUIUtility.GetDefaultTextColor();
            variable.normal.background = Resources.Load<Texture2D>("Dark/SDPD_NormalBg");

            _storage.Add("SerializableDictionary.PairDisplayer.NormalBackground.Dark", variable);

            variable = new GUIStyle() {
                border = new RectOffset(2, 2, 2, 2),
                fixedWidth = 10,
                fixedHeight = 10,
            };

            variable.normal.background = Resources.Load<Texture2D>("Dark/Searchbar_NormalCancelButton");
            variable.hover.background = Resources.Load<Texture2D>("Dark/Searchbar_HoverCancelButton");

            _storage.Add("SerializableDictionary.ControlPanel.Searchbar.CancelButton.Dark", variable);
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
    }
}