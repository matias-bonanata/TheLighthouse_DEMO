using System;
using TMPro;
using UnityEngine;

namespace FastStudios.Demo
{
    [RequireComponent(typeof(Interactable))]
    public class KeyPrompt : MonoBehaviour
    {
        public string DebugFailureDefault = "Missing Keys: {keys}";
        public TMP_Text TextToDisplay;
        public GameObject gameObjectToActivate;

        private Interactable thisInteractable;
        private string keysToShow;

        void Awake()
        {
            thisInteractable = GetComponent<Interactable>();

            DeactiveGameObject();
        }
        
        public void DebugFailure(string message)
        {
            if (String.IsNullOrEmpty(message)) message = DebugFailureDefault;

            keysToShow = InteractionManager.singleton.MissingKeysString(thisInteractable);

            if (message.Contains("{keys}"))
            {
                message = message.Replace("{keys}", keysToShow);
            }

            TextToDisplay.text = message;
            gameObjectToActivate.SetActive(true);

            Invoke(nameof(DeactiveGameObject), 2f);
        }

        void DeactiveGameObject()
        {
            gameObjectToActivate.SetActive(false);
        }
    }
}
