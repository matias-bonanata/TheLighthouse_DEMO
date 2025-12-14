using UnityEngine;

public class QtoTurnOnLight : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private AudioClip lightTurnOn;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            targetObject.SetActive(!targetObject.activeSelf);
            SoundManager.instance.PlayWaitSoundFXClip(lightTurnOn, transform, 1f);
        }
    }
}
