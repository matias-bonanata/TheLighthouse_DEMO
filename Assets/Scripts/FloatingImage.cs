using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private MMF_Player MMF_Player;
    [SerializeField] private Transform player;
    [SerializeField] private Transform pointToFloatFrom;
    [SerializeField] private float floatHeight = 2f;
    [SerializeField] private float floatAmplitude = 0.5f; // How high the object floats up and down
    [SerializeField] private float floatFrequency = 1f;
    [SerializeField] private float pullStrength = 0.1f;

    [Header("Animation Settings")]
    [SerializeField] private Animator UIanimator;
    [SerializeField] private ConversationStarter conversationStarter;
    [SerializeField] private string openTrigger = "Open Animation";
    [SerializeField] private string closeTrigger = "Close Animation";
    public bool canInteract = false;

    private Vector3 basePosition;
    private bool isHovered = false;
    private float originalAmplitude;



    void Start()
    {
        originalAmplitude = floatAmplitude; // Store original value
    }

    void Update()
    {
        if (pointToFloatFrom == null)
            return;

        // Use amplitude 0 when hovered, original when not
        float currentAmplitude = isHovered ? 0f : originalAmplitude;

        // Base position is NPC position plus fixed height offset
        basePosition = pointToFloatFrom.position + Vector3.up * floatHeight;

        // Floating up and down on y-axis with sine wave (stops when hovered)
        float newY = basePosition.y + Mathf.Sin(Time.time * Mathf.PI * floatFrequency) * currentAmplitude;

        Vector3 horizontalPos = new Vector3(basePosition.x, 0, basePosition.z);

        if (pullStrength > 0 && player != null)
        {
            Vector3 directionToPlayer = player.position - pointToFloatFrom.position;
            Vector3 horizontalPull = new Vector3(directionToPlayer.x, 0, directionToPlayer.z).normalized * pullStrength;
            horizontalPos += horizontalPull;
        }

        transform.position = new Vector3(horizontalPos.x, newY, horizontalPos.z);

        //close it when you exit collider
        if (conversationStarter.insideCollider == false)
        {
            CloseAnimation();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (conversationStarter.insideCollider == true)
        {
            OpenAnimation();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
            CloseAnimation();
    }

    private void OpenAnimation()
    {
        // Stop floating (amplitude becomes 0 in Update)
        if (isHovered) return;
        isHovered = true;

        // Trigger Open Animation
        if (UIanimator != null)
            UIanimator.SetTrigger(openTrigger);

        MMF_Player.PlayFeedbacks();
    }

    private void CloseAnimation()
    {
        // Resume floating (amplitude goes back to original in Update)
        if (!isHovered) return;
        isHovered = false;

        // Trigger Close Animation
        if (UIanimator != null)
            UIanimator.SetTrigger(closeTrigger);

        MMF_Player.PlayFeedbacks();
    }

    public void canClick()
    {
        canInteract = true;
    }

    public void disableClick()
    {
        canInteract = false;
    }
}