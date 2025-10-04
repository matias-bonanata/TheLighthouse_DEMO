using UnityEngine;
using DialogueEditor;
using TMPro;

public class ConversationStarter : MonoBehaviour
{
    [Header("What to do when Interact")]
    [SerializeField] private NPCConversation conversation;
    public Transform teleportLocation;
    public GameObject thingToDestroy;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI uiText;
    [SerializeField] private string message = "x";
    public PlayerInteractUI playerInteractUI;

    [Header("Player Position for Teleport")]
    [SerializeField] public CharacterController player;

    private bool willConverse = false;
    private bool willTeleport = false;
    private bool willDelete = false;

    void Start()
    {
        if (conversation != null)
        {
            willConverse = true;
        }
        else
        {
            willConverse = false;
        }

        if (teleportLocation != null && player != null)
        {
            willTeleport = true;
        }
        else
        {
            willTeleport = false;
        }

        if (thingToDestroy != null)
        {
            willDelete = true;
        }
        else
        {
            willDelete = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerInteractUI != null)
            {
                playerInteractUI.ShowContainer();
                uiText.text = message;
            }

            //E to Interact
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (willConverse == true)
                {
                    ConversationManager.Instance.StartConversation(conversation);
                    playerInteractUI.HideContainer();
                }

                if (willTeleport == true)
                {
                    player.enabled = false;
                    player.transform.position = teleportLocation.position;
                    player.enabled = true;
                    playerInteractUI.HideContainer();
                }

                if (willDelete == true)
                {
                    thingToDestroy.SetActive(false);
                    uiText.text = " ";
                    transform.parent.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerInteractUI != null)
                playerInteractUI.HideContainer();
        }
    }




   

    void Update()
    {
        
    }
}
