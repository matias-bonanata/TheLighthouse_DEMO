using UnityEngine;

namespace FastStudios
{
    public class BlockPlacementArea : MonoBehaviour
    {
        [HideInInspector] public bool Block = true;

        [HideInInspector] public BoxCollider boxCollider;

        void Awake()
        {
            SetupBoxCollider();
        }

        void SetupBoxCollider()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }

    #if UNITY_EDITOR

        void OnValidate()
        {
            SetupBoxCollider();
        }

    #endif
    }

}