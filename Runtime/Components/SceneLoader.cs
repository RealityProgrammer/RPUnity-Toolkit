using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealityProgrammer.UnityToolkit.Core.Miscs;

namespace RealityProgrammer.UnityToolkit.Core.Components {
    public class SceneLoader : MonoBehaviour {
        [SerializeField] SceneReference scene;

        [SerializeField] bool startOnAwake = false;
        [SerializeField] bool async = false;
        [SerializeField] LoadSceneMode mode = LoadSceneMode.Single;

        private void Awake() {
            if (startOnAwake) {
                Invoke();
            }
        }

        public void Invoke() {
            if (scene != null) {
                if (async) {
                    SceneManager.LoadSceneAsync(scene, mode);
                } else {
                    SceneManager.LoadScene(scene, mode);
                }
            }
        }
    }
}