using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class AnchorChanger : MonoBehaviour
    {
#if UNITY_EDITOR
        public Interactable currentInteractable;
        public AnchorChangerType anchorChangerType;
        public float VisualSphereRadius = 0.05f;
        public bool AutoDeleteWhenUnselected = true;

        [HideInInspector] public Vector3 oldValue;

        public void Setup(Interactable parent, AnchorChangerType anchorChanger, ref bool WillOverride, ref Vector3 LocalPos, ref bool isOverriding)
        {
            currentInteractable = parent;
            anchorChangerType = anchorChanger;
            oldValue = LocalPosNewAnchorVec3();

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                if (parent.transform.GetChild(i).GetComponent<AnchorChanger>() == null) continue;

                DestroyImmediate(parent.transform.GetChild(i).gameObject);
            }

            transform.SetParent(parent.transform, false);

            transform.localPosition = Vector3.zero;

            if (WillOverride && LocalPos != Vector3.zero) transform.localPosition = LocalPos;
            transform.localEulerAngles = Vector3.zero;

            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);

            isOverriding = true;
        }

        void Update()
        {
            if (currentInteractable != null)
            {
                if (GetIsOverriding())
                {
                    if (Selection.activeGameObject != gameObject && AutoDeleteWhenUnselected)
                    {
                        End(true);
                    }
                    else
                    {
                        LocalPosNewAnchorVec3() = transform.localPosition;
                    }
                }
            }
        }

        public void End(bool apply)
        {
            if (apply) LocalPosNewAnchorVec3() = transform.localPosition;
            else LocalPosNewAnchorVec3() = oldValue;

            Selection.activeGameObject = currentInteractable.gameObject;
            EditorGUIUtility.PingObject(currentInteractable.gameObject);

            GetIsOverriding() = false;
            DestroyImmediate(gameObject);
        }

        public void Cancel() => End(false);
        public void Apply() => End(true);
        public void Restore()
        {
            transform.localPosition = oldValue;
        }

        void OnDrawGizmos()
        {
            if (currentInteractable != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(currentInteractable.transform.TransformPoint(LocalPosNewAnchorVec3()), VisualSphereRadius);
            }
        }

        private bool errorBool;

        public ref bool GetIsOverriding()
        {
            switch (anchorChangerType)
            {
                case AnchorChangerType.AnchorPosition:

                    return ref currentInteractable.isOverridingAnchorPosition;

                case AnchorChangerType.WorldUIAnchor:

                    return ref currentInteractable.isOverridingWorldUIAnchorPosition;

                case AnchorChangerType.DragUIAnchor:

                    return ref currentInteractable.isOverridingDragUIAnchorPosition;

                case AnchorChangerType.HoldUIAnchor:

                    return ref currentInteractable.isOverridingHoldUIAnchorPosition;
            }

            return ref errorBool;
        }

        private Transform errorTransform;

        private Vector3 errorVec3;
        public ref Vector3 LocalPosNewAnchorVec3()
        {
            switch (anchorChangerType)
            {
                case AnchorChangerType.AnchorPosition:

                    return ref currentInteractable.LocalPositionNewAnchor;

                case AnchorChangerType.WorldUIAnchor:

                    return ref currentInteractable.LocalPositionWorldUIAnchor;

                case AnchorChangerType.DragUIAnchor:

                    return ref currentInteractable.LocalPositionDragUIAnchor;

                case AnchorChangerType.HoldUIAnchor:

                    return ref currentInteractable.LocalPositionHoldUIAnchor;
            }

            return ref errorVec3;
        }

        [MenuItem("CONTEXT/AnchorChanger/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

#else
        void Awake()
        {
            Destroy(this);
        }
#endif
    }
}
