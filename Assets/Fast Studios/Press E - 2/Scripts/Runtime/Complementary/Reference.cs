using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
    public class Reference : MonoBehaviour
    {
        public GameObject playerObject;
        public bool autoAssignThisObject = false;

        void Assign()
        {
            if (autoAssignThisObject) playerObject = gameObject;

            InteractionManager singleton = InteractionManager.singleton;
            AssignPlayerToInteractionManager(singleton);

            if (singleton != null && singleton.PlayerDetection != PlayerDetection.Reference)
            {
                Debug.LogWarning("[Press E] Trying to reference player object with a reference script, when player detection in manager is not reference");
            }
        }

        public void Awake() => Assign();

        public void Start() => Assign();

        public void Update()
        {
            InteractionManager singleton = InteractionManager.singleton;

            if (singleton != null && singleton.playerObject == null && singleton.PlayerDetection == PlayerDetection.Reference)
            {
                Assign();
            }
        }

        public void AssignPlayerToInteractionManager(InteractionManager singleton)
        {
            if (singleton == null) return;

            singleton.playerObject = playerObject;

            if (singleton.playerCollider == null) singleton.TryGetPlayerCollider();
        }

#if UNITY_EDITOR

        [MenuItem("CONTEXT/Reference/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

#endif
    }
}
