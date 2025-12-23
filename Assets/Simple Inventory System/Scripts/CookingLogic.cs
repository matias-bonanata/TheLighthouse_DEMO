using System.Linq;
using RedstoneinventeGameStudio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CookingLogic : MonoBehaviour
{
    [SerializeField] CardManager[] targetCards = new CardManager[5];  // 5 target cards
    [SerializeField] Image[] destinationImages = new Image[5];       // 5 destination images
    [SerializeField] Image fillBarImage;
    [SerializeField] TextMeshProUGUI fillDescriptionText;

    [SerializeField] Sprite beansImage, carrotImage, fishImage, potatoImage;

    private float totalFillAmount = 0f;  // Tracks stacked fill across all cards

    private bool anyCardOccupied = false;

    void Update()
    {
        ReadItemDataFromAllCards();
    }

    private void ReadItemDataFromAllCards()
    {
        totalFillAmount = 0f;  // Reset and recalculate total each frame

        // Process all 5 cards
        for (int i = 0; i < targetCards.Length; i++)
        {
            CardManager card = targetCards[i];
            if (card == null) continue;

            // Handle individual destination image for this card
            if (destinationImages[i] != null)
            {
                if (card.isOccupied && card.itemData?.itemIcon != null)
                {
                    anyCardOccupied = true;

                    // Set sprite based on this card's iconName
                    string iconName = card.itemData.itemIcon.name;
                    Sprite spriteToShow = GetSpriteForIcon(iconName);
                    destinationImages[i].sprite = spriteToShow;
                    destinationImages[i].color = new Color(1f, 1f, 1f, 1f);  // Opaque

                    // Stack fill amounts from ALL cards
                    float fillIncrement = GetFillIncrement(iconName);
                    totalFillAmount += fillIncrement;
                }
                else
                {
                    // Reset when unoccupied
                    destinationImages[i].color = new Color(1f, 1f, 1f, 0f);  // Transparent
                }
            }
        }

        // Apply stacked total to single fill bar
        if (fillBarImage != null)
        {
            fillBarImage.fillAmount = Mathf.Clamp01(totalFillAmount);
        }

        UpdateFillDescriptionText();
    }

    private float GetFillIncrement(string iconName)
    {
        return iconName switch
        {
            "bean" => 0.3f,
            "carrot" => 0.15f,
            "fish" => 0.3f,
            "potato" => 0.30f,
            _ => 0f
        };
    }

    private Sprite GetSpriteForIcon(string iconName)
    {
        return iconName switch
        {
            "bean" => beansImage,
            "carrot" => carrotImage,
            "fish" => fishImage,
            "potato" => potatoImage,
            _ => null  // No sprite if unknown icon
        };
    }

    private void UpdateFillDescriptionText()
    {
        if (fillDescriptionText == null) return;

        float clampedFill = Mathf.Clamp01(totalFillAmount);

        if (clampedFill >= 0.99f)
        {
            fillDescriptionText.text =
                "Okay now slow down, this is a meal to remember. Try and conserve the food we have, there ain't much.";
        }
        if (clampedFill > 0.6f)
        {
            fillDescriptionText.text = 
                "Now we're talking! Something to keep the strong body kicking, and the speeds running full power.";
        }
        else if (clampedFill > 0.3f)
        {
            fillDescriptionText.text = 
                "Something or the other, it's shaping like a fine meal to keep you strong during the day.";
        }
        else if (clampedFill > 0f)
        {
            fillDescriptionText.text = 
                "Hey, at least it's food. It'll keep you up and running for a short while.";
        }
        else
        {
            fillDescriptionText.text = "An empty plate, full of sadness and misery.";
        }
    }
}
