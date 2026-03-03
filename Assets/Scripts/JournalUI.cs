using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    [SerializeField] private Image journalImage;           // Main journal page Image
    [SerializeField] private Sprite[] pages;               // Journal page sprites
    [SerializeField] private Button nextButton;            // Next button
    [SerializeField] private Button previousButton;        // Previous button
    [SerializeField] private GameObject targetObject;      // Object to activate/deactivate
    [SerializeField] private Image secondaryImage;         // Secondary Image to change sprites
    [SerializeField] private Sprite[] secondarySprites;    // Sprites for secondary Image per page

    private int currentPage = 0;

    private void Start()
    {
        nextButton.onClick.AddListener(NextPage);
        previousButton.onClick.AddListener(PreviousPage);
        UpdatePage();
    }

    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            UpdatePage();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
        else if (currentPage == 0)
        {
            transform.parent.gameObject.SetActive(false);
        }
    }

    private void UpdatePage()
    {
        journalImage.sprite = pages[currentPage];

        // Handle target object state (page 0 = active, page 1+ = inactive)
        if (targetObject != null)
        {
            targetObject.SetActive(currentPage == 0);
        }

        // Change secondary image sprite per page
        if (secondaryImage != null && secondarySprites != null && currentPage < secondarySprites.Length)
        {
            secondaryImage.sprite = secondarySprites[currentPage];
        }
    }
}
