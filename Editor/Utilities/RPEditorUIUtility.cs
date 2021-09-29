using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RealityProgrammer.UnityToolkit.Editors.Utility {
    public static class RPEditorUIUtility {
        public static Color GetDefaultTextColor() {
            return EditorGUIUtility.isProSkin ? new Color32(0xD2, 0xD2, 0xD2, 255) : new Color32(0x14, 0x14, 0x14, 255);
        }
    }
}