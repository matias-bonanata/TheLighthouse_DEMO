using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class BoatToggle : MonoBehaviour
{
    [Header("References (Assign in Inspector)")]
    [SerializeField] private GameObject player;
    [SerializeField] private CinemachineCamera boatCamera;
    [SerializeField] private MonoBehaviour boatScript;

    [Header("Settings")]
    [SerializeField] private float cooldownDuration = 2f;

    private bool isPlayerInTrigger = false;
    private bool isActivated = false;
    private bool isCooldownActive = false;
    private int originalCameraPriority;

    private void Start()
    {
        // Store original camera priority
        if (boatCamera != null)
        {
            originalCameraPriority = boatCamera.Priority;
        }

        // Ensure script is initially inactive if needed
        if (boatScript != null)
        {
            boatScript.enabled = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
        }
    }

    private void Update()
    {
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.E) && !isCooldownActive)
        {
            ToggleInteraction();
        }
    }

    private void ToggleInteraction()
    {
        if (isActivated)
        {
            // Revert changes
            TogglePlayer();
        }
        else
        {
            // Apply changes
            ToggleBoat();
        }

        // Start cooldown
        StartCoroutine(CooldownCoroutine());
    }

    private void ToggleBoat()
    {
        // Deactivate player
        if (player != null)
        {
            player.SetActive(false);
        }

        // Change camera priority to 10
        if (boatCamera != null)
        {
            boatCamera.Priority = 10;
        }

        // Activate boat script
        if (boatScript != null)
        {
            boatScript.enabled = true;
        }

        isActivated = true;
    }

    private void TogglePlayer()
    {
        // Reactivate player
        if (player != null)
        {
            player.SetActive(true);
        }

        // Restore original camera priority
        if (boatCamera != null)
        {
            boatCamera.Priority = originalCameraPriority;
        }

        // Deactivate boat script
        if (boatScript != null)
        {
            boatScript.enabled = false;
        }

        isActivated = false;
    }

    private IEnumerator CooldownCoroutine()
    {
        isCooldownActive = true;
        yield return new WaitForSeconds(cooldownDuration);
        isCooldownActive = false;
    }
}
