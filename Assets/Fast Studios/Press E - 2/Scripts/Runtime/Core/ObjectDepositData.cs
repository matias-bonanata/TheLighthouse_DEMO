using System.Collections.Generic;
using UnityEngine;

namespace FastStudios
{
    [System.Serializable]
    public class ObjectDepositData
    {
        public GameObject gameObject;
        public Vector3 Position = Vector3.zero;
        public Vector3 Rotation = Vector3.zero;
        public Vector3 Scale = Vector3.one;

        public bool CanStack;
        public bool HasStackLimit;
        [Min(2)] public int ActualStackLimit = 2;
        public bool HasVisualStackLimit;
        [Min(1)] public int VisualStackLimit = 1;
        public Vector3 DistanceBetween;
        [HideInInspector] public List<GameObject> StacksList = new List<GameObject>();

        [HideInInspector] public bool IsNullObject = false;
        [HideInInspector] public bool IsAttatched = false;
    }

}