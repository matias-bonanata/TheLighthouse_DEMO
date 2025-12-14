using DialogueEditor;
using Unity.Cinemachine;
using UnityEngine;
using System.Collections;

public class StartIntroConversation : MonoBehaviour
{
    [SerializeField] private NPCConversation conversation;
    [SerializeField] private GameObject itemToggle;
    [SerializeField] private CinemachineCamera cameraToPrioritise;
    [SerializeField] private CinemachineCamera cameraToUnPrioritise;
    [SerializeField] private GameObject gameObjectToDisable;
    [SerializeField] private GameObject gameObjectToEnable;


    void Start()
    {
        ConversationManager.Instance.StartConversation(conversation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void changeCamera()
    {
        StartCoroutine(SwitchCamerainSeconds());
    }

    private IEnumerator SwitchCamerainSeconds()
    {
        yield return new WaitForSeconds(5f);

        gameObjectToEnable.SetActive(true);
        cameraToPrioritise.Priority = 5;
        cameraToUnPrioritise.Priority = 0;
        itemToggle.SetActive(true);
        gameObjectToDisable.SetActive(false);
    }
}
