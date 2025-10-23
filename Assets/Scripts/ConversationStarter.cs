using UnityEngine;
using DialogueEditor;
using TMPro;
using System.Collections;

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

    [Header("Hold to Delete")]
    [SerializeField] private float holdTimer = 0f; // 5 seconds
    [SerializeField] private float holdTime = 3f; // 5 seconds

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
            }

            if (Input.GetKey(KeyCode.E))
            {
                if (willDelete == true)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdTime)
                    {
                        DeleteParent();
                        holdTimer = 0f; // reset or disable further deletion
                    }
                }
                else
                {
                    holdTimer = 0f; // reset timer if key released
                }
            }
            if (Input.GetKeyUp(KeyCode.E)) holdTimer = 0f; // reset timer if key released
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerInteractUI != null)
                playerInteractUI.HideContainer();

            //if is Holding, reset
            holdTimer = 0f;
        }
    }

    void DeleteParent()
    {
        //DELETE
        thingToDestroy.SetActive(false);
        uiText.text = " "; //don't show any text
        transform.parent.gameObject.SetActive(false);
    }

    void Update()
    {
        
    }
}
