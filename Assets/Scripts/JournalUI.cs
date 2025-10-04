using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    [SerializeField] private Image journalImage; // Assign your UI Image that shows the journal page
    [SerializeField] private Sprite[] pages;     // Assign all journal page sprites in order in the inspector

    private int currentPage = 0;

    private void Start()
    {
        UpdatePage();
    }

    private void Update()
    {
        //if (gameObject.activeSelf)
        //{
        //    playerMovement.enabled = false;
        //}
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            NextPage();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            PreviousPage();
        }
    }

    private void NextPage()
    {
        if (currentPage < pages.Length - 1)
        {
            currentPage++;
            UpdatePage();
        }
        // If on last page, do nothing on right input (no bound overflow)
    }

    private void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePage();
        }
        else if (currentPage == 0)
        {
            // No more pages to the left, deactivate journal object
            transform.parent.gameObject.SetActive(false);
        }
    }

    private void UpdatePage()
    {
        journalImage.sprite = pages[currentPage];
    }

    //public void disableParent()
    //{
    //    transform.parent.gameObject.SetActive(false);
    //}
}
