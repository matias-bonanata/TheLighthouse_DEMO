using UnityEngine;
using DialogueEditor;
using TMPro;
using System.Collections;
using Unity.Cinemachine;
using Unity.Collections.LowLevel.Unsafe;

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
    [SerializeField] public AudioClip teleportSound;

    [Header("Hold to Delete")]
    [SerializeField] private float holdTimer = 0f; // 5 seconds
    [SerializeField] private float holdTime = 3f; // 5 seconds
    [SerializeField] private AudioClip deleteSound;

    private bool willConverse = false;
    private bool willTeleport = false;
    private bool willDelete = false;
    private bool willChangeCamera = false;
    private bool willInitialiseObject = false;
    private bool willToggleObject = false;

    [Header("Camera Change")]
    [SerializeField] private CinemachineCamera cameraToEdit;
    [SerializeField] private int priorityNumber = 0;

    [Header("Object to Initialise")]
    [SerializeField] private GameObject objectToInitialise;
    [SerializeField] private MonoBehaviour scriptToDisable;

    [Header("Toggle Object")]
    [SerializeField] private GameObject objectToToggle1;
    [SerializeField] private GameObject objectToToggle2;
    private bool isObject1Toggled = false;
    private bool isObject2Toggled = false;

    [Header("Should Fade")]
    [SerializeField] private FadeBlackScreen fadeScript;

    [Header("Mental Meter")]
    [SerializeField] private MentalMeter mentalMeter;

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

        if (cameraToEdit != null)
        {
            willChangeCamera = true;
        }
        else
        {
            willChangeCamera = false;
        }

        if (thingToDestroy != null)
        {
            willDelete = true;
        }
        else
        {
            willDelete = false;
        }

        if (objectToInitialise != null)
        {
            willInitialiseObject = true;
        }
        else
        {
            willInitialiseObject = false;
        }

        if ((objectToToggle1 != null || objectToToggle2 != null))
        {
            willToggleObject = true;
            if (objectToToggle1 != null) isObject1Toggled = objectToToggle1.activeSelf;
            if (objectToToggle2 != null) isObject2Toggled = objectToToggle2.activeSelf;
        }
        else
        {
            willToggleObject = false;
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
                    if (!SoundManager.instance.IsSoundPlaying(teleportSound) && 
                        teleportSound != null)
                    {
                        SoundManager.instance.PlayWaitSoundFXClip(teleportSound, transform, 1f);
                    }

                    if (fadeScript != null) fadeScript.StartFadeSequence();
                    player.enabled = false;
                    player.transform.position = teleportLocation.position;
                    player.enabled = true;
                    playerInteractUI.HideContainer();
                }

                if (willChangeCamera == true)
                {
                    cameraToEdit.Priority = priorityNumber;
                }

                if (willInitialiseObject == true)
                {
                    objectToInitialise.SetActive(true);
                    if (scriptToDisable != null) scriptToDisable.enabled = false;
                    NPCFloatingUI targetScript = GetComponent<NPCFloatingUI>();
                    if (targetScript != null)
                    {
                        targetScript.activationDistance = 0f;  // Change the value here
                    }
                }

                if (willToggleObject == true)
                {
                    ToggleObjects();
                }

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

    private void DeleteParent()
    {
        if (!SoundManager.instance.IsSoundPlaying(deleteSound) &&
    deleteSound != null)
        {
            SoundManager.instance.PlayWaitSoundFXClip(deleteSound, transform, 1f);
        }

        //DELETE
        increaseMental();
        thingToDestroy.SetActive(false);
        uiText.text = " "; //don't show any text
        //transform.parent.gameObject.SetActive(false);
    }

    private void ToggleObjects()
    {
        if (objectToToggle1 != null)
        {
            isObject1Toggled = !isObject1Toggled;
            objectToToggle1.SetActive(isObject1Toggled);
        }

        if (objectToToggle2 != null)
        {
            isObject2Toggled = !isObject2Toggled;
            objectToToggle2.SetActive(isObject2Toggled);
        }
    }

    public void increaseMental()
    {
        if (mentalMeter != null)
        {
            mentalMeter.UpdateHealth("Recover", 2);
            mentalMeter.ChangeMentalBarColor(Color.green);
        }

    }

    public void decreaseMental()
    {
        if (mentalMeter != null)
        {
            mentalMeter.UpdateHealth("Damage", 2);
            mentalMeter.ChangeMentalBarColor(Color.red);
        }
    }
}
