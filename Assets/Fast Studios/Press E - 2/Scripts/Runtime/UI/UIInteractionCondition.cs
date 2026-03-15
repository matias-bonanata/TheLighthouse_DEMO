using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
    public class UIInteractionCondition : MonoBehaviour
    {
        public UIPrefab uIPrefab;

        public InteractMode TargetInteractMode = InteractMode.UnityEvent;
        public bool WhenTargetInteractMode = true;

        void Awake()
        {
            if (uIPrefab == null) uIPrefab = GetComponentInParent<UIPrefab>();

            if (uIPrefab == null)
            {
                Debug.LogWarning("[Press E] UI Prefab is null, please assign in the inspector");
            }
        }

        void Start()
        {
            if (uIPrefab != null && uIPrefab.interactedInteractable.interactMode == TargetInteractMode)
            {
                gameObject.SetActive(WhenTargetInteractMode);
            }
            else if (uIPrefab != null && uIPrefab.interactedInteractable == null)
            {
                Debug.LogWarning("[Press E ] System didnt found the interactable object that made this UI show up, trying to search again in 0.5s");
                Invoke(nameof(Start), 0.5f);
            }
            else
            {
                gameObject.SetActive(!WhenTargetInteractMode);
            }
        }

#if UNITY_EDITOR

        [MenuItem("CONTEXT/UIInteractionCondition/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

        void Reset()
        {
            if (uIPrefab == null) uIPrefab = GetComponentInParent<UIPrefab>();

            if (uIPrefab == null)
            {
                Debug.LogWarning("[Press E] Couldnt find UI Prefab automatically, please assign in the inspector");
            }
        }
        
        #endif
    }
}
