using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;
using UnityEngine.Rendering;

public class ClickOnFloatingUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private MonoBehaviour scriptToToggle;
    [SerializeField] private Animator animator;
    public bool canInteract;

    private bool isHovered = false;

    void Start()
    {
        if (scriptToToggle != null)
            scriptToToggle.enabled = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered) return;
        isHovered = true;

        // Disable script
        if (scriptToToggle != null)
            scriptToToggle.enabled = false;

        // Trigger "Open Animation"
        if (animator != null)
            animator.SetTrigger("OpenAnimation");

        canInteract = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered) return;
        isHovered = false;

        // Enable script
        if (scriptToToggle != null)
            scriptToToggle.enabled = true;

        // Trigger "Close Animation"
        if (animator != null)
            animator.SetTrigger("CloseAnimation");

        canInteract = false;
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
