using Unity.Cinemachine;
using UnityEngine;

public class PrivateVoidChangeCamera : MonoBehaviour
{
    [SerializeField] public CinemachineCamera cameraToPrioritise;
    [SerializeField] public int priorityNumber;
    [SerializeField] private FadeBlackScreen fadeScript;


    public void ChangeCameraPriority()
    {
        if (fadeScript != null) fadeScript.StartFadeSequence();
        cameraToPrioritise.Priority = priorityNumber;
    }
}
