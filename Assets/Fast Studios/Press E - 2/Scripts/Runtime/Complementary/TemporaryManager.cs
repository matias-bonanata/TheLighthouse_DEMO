using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class TemporaryManager : MonoBehaviour
    {

#if UNITY_EDITOR

        [MenuItem("CONTEXT/TemporaryManager/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

        private void OnEnable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += DestroyThisObject;
        }

        private void OnDisable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= DestroyThisObject;
        }


        void DestroyThisObject(Scene scene, bool removingScene)
        {
            if (EditorApplication.isPlaying == false)
            {
                DestroyImmediate(gameObject);
            }
        }

#else

        public void Awake()
        {
            Destroy(this);
        }

#endif
    }
}
