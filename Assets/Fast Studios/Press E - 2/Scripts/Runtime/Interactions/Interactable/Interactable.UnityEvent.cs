using UnityEngine;
using UnityEngine.Events;

namespace FastStudios
{
    public partial class Interactable // Unity Event
    {
        #region Unity Event Public
        public UnityEvent unityEventToTrigger;
        [HideInInspector] public bool oneTimeUnityEventBool = false;
        #endregion

    }
}