using Unity.Cinemachine;
using UnityEngine;

public class IfCollidePrioritiseCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cameraToPrioritise;
    [SerializeField] private CinemachineCamera secondCameraToPrioritise;
    [SerializeField] private int priorityNumber;
    [SerializeField] private int secondPriorityNumber;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cameraToPrioritise.Priority = priorityNumber;
            secondCameraToPrioritise.Priority = secondPriorityNumber;
        }
    }
}
