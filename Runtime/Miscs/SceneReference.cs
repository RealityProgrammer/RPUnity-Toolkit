using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace RealityProgrammer.UnityToolkit.Core.Miscs {
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver {
#if UNITY_EDITOR
        [SerializeField] SceneAsset sceneAsset;
#endif
        [SerializeField] protected string scenePath;

        public static implicit operator string(SceneReference scene) {
            return scene.scenePath;
        }

        public void OnAfterDeserialize() {
#if UNITY_EDITOR
            EditorApplication.update += DeserializeCall;
#endif
        }

#if UNITY_EDITOR
        private void DeserializeCall() {
            EditorApplication.update -= DeserializeCall;

            if (string.IsNullOrEmpty(scenePath)) return;

            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            if (!sceneAsset) scenePath = string.Empty;
            if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();
        }
#endif

        public void OnBeforeSerialize() {
#if UNITY_EDITOR
            if (sceneAsset == null) {
                if (!string.IsNullOrEmpty(scenePath)) {
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (sceneAsset == null) scenePath = string.Empty;

                    EditorSceneManager.MarkAllScenesDirty();
                }
            } else {
                scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            }
#endif
        }
    }
}