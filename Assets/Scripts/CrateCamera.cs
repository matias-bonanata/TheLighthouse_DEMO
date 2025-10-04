using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;

public class CrateCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera crateCamera;
    private bool playerInside = false;
    //private ThirdPersonController playerMovement;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            // Optionally reset priority when leaving
            crateCamera.Priority = 0;
        }
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            // Toggle priority between 0 and 5 (or any values that fit your priorities)
            crateCamera.Priority = (crateCamera.Priority == 0) ? 5 : 0;
        }
    }
}
