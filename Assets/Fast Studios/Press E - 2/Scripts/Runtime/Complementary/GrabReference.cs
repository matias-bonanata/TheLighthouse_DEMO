using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
    public class GrabReference : MonoBehaviour
    {
        public bool autoAssignThisTransform = true;
        public Transform GrabTransform;

        void Assign()
        {
            if (autoAssignThisTransform) GrabTransform = transform;

            AssignPlayerToInteractionManager();

            InteractionManager singleton = InteractionManager.singleton;
            if (singleton != null && singleton.UseGrabReference == false)
            {
                Debug.LogWarning("[Press E] Trying to reference grab transform object with a reference script, when player reference mode is disabled in Interaction Manager");
            }
        }

        public void Awake() => Assign();

        public void Start() => Assign();

        public void Update()
        {
            InteractionManager singleton = InteractionManager.singleton;

            if (singleton != null && singleton.UseGrabReference && singleton.ItemPlayerGrabTransform == null)
            {
                Assign();
            }
        }

        public void AssignPlayerToInteractionManager()
        {
            InteractionManager singleton = InteractionManager.singleton;

            if (singleton == null) return;

            singleton.ItemPlayerGrabTransform = GrabTransform;
        }

#if UNITY_EDITOR

        [MenuItem("CONTEXT/GrabReference/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

#endif
    }
}
