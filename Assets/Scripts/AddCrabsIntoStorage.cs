using UnityEngine;
using RedstoneinventeGameStudio;
using System.Collections.Generic;
using System.Linq;

public class AddCrabsIntoStorage : MonoBehaviour
{
    [SerializeField] private GameObject[] groupedObjects;   // Your group of 3 active + 2 inactive objects
    [SerializeField] private CardManager[] cards;           // Your cooking cards array
    [SerializeField] private InventoryItemData itemToAdd;   // Same item for all active objects
    [SerializeField] private GameObject MenuContainer;   // Same item for all active objects

    private void OnEnable()
    {
        FillCardsWithActiveObjectCount();
    }

    [ContextMenu("Fill Cards Based On Active Objects")]
    public void FillCardsWithActiveObjectCount()
    {
        int activeObjectCount = CountActiveObjects();
        Debug.Log($"Found {activeObjectCount} active objects - filling {activeObjectCount} cards");

        List<CardManager> availableCards = GetUnoccupiedCards();

        // Fill up to the number of active objects (or available cards)
        for (int i = 0; i < activeObjectCount && i < availableCards.Count; i++)
        {
            CardManager targetCard = availableCards[i];  // Sequential fill, or randomize below
            bool success = targetCard.SetItem(itemToAdd);

            if (success)
            {
                Debug.Log($"Added {itemToAdd.itemName} to card {i + 1}");
            }
        }
        DeactivateAllGroupedObjects();
    }

    // RANDOM VERSION - uncomment to fill random cards instead of sequential
    /*
    [ContextMenu("Fill Random Cards Based On Active Objects")]
    public void FillRandomCardsWithActiveObjectCount()
    {
        int activeObjectCount = CountActiveObjects();
        Debug.Log($"Found {activeObjectCount} active objects - filling {activeObjectCount} random cards");
        
        List<CardManager> availableCards = GetUnoccupiedCards();
        
        for (int i = 0; i < activeObjectCount && availableCards.Count > 0; i++)
        {
            CardManager targetCard = availableCards[Random.Range(0, availableCards.Count)];
            availableCards.Remove(targetCard);  // Remove to avoid duplicates
            
            bool success = targetCard.SetItem(itemToAdd);
            if (success)
            {
                Debug.Log($"Added {itemToAdd.itemName} to random card");
            }
        }
    }
    */

    private int CountActiveObjects()
    {
        return groupedObjects.Count(obj => obj != null && obj.activeInHierarchy);
    }

    private List<CardManager> GetUnoccupiedCards()
    {
        return cards.Where(card => card != null && !card.isOccupied).ToList();
    }
    private void DeactivateAllGroupedObjects()
    {
        foreach (GameObject obj in groupedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}
