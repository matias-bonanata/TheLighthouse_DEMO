using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ExternPropertyAttributes;

public class MentalMeter : MonoBehaviour
{
    [Header("Health Controls")]
    public float playerHealth;
    [SerializeField] private float maxHealth;
    [SerializeField] private int damageMult;

    [Header("Hungry")]
    [SerializeField] private float playerHunger; //ability to stay hungry
    [SerializeField] private float hungerDrain; //how fast hunger drains
    [SerializeField] private float maxHunger; //maximum amount of value
    [SerializeField] private float hungerTolerance; //how tolerant

    [Header("Tired")]
    [SerializeField] private float playerTired;
    [SerializeField] private float tiredDrain;
    [SerializeField] private float maxTired;
    [SerializeField] private float tiredTolerance;

    [Header("Cold")]
    [SerializeField] private float playerCold;
    [SerializeField] private float coldDrain;
    [SerializeField] private float maxCold;
    [SerializeField] private float coldTolerance;
    private bool isCold = false;

    [Header("References")]
    [SerializeField] private Image mentalBar;
    [SerializeField] private Image playerImage;
    [SerializeField] private FadeBlackScreen fadeScript;

    private bool isColorOverridden = false;

    void Start()
    {
        fadeScript.StartInstantFadeSequence();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            UpdateHealth("Recover", 2);
            isCold = true;
            ChangeMentalBarColor(Color.red);
        }

        if (playerHealth >= 0)
        {
            //hunger
            if (playerHunger >= 0)
            {
                playerHunger -= Time.deltaTime * hungerDrain;
            }
            else
            {
                UpdateHealth("Suffer", damageMult);
            }

            //tired
            if (playerTired >= 0)
            {
                playerTired -= Time.deltaTime * tiredDrain;
            }
            else
            {
                UpdateHealth("Suffer", damageMult);
            }

            //cold
            if (isCold)
            {
                if (playerCold >= 0)
                {
                    playerCold -= Time.deltaTime * coldDrain;
                }
                else
                {
                    UpdateHealth("Suffer", damageMult);
                }
            }
            else
            {
                playerCold = maxCold;
            }
        }

        UpdateVitals("Normal", 0);
        UpdateMentalBarColor();
    }

    public void UpdateVitals(string vitalType, int value)
    {
        if (vitalType == "Hunger")
        {
            playerHunger += value;

            if (playerHunger >= maxHunger)
            {
                playerHunger = maxHunger;
            }
        }

        if (vitalType == "Tired")
        {
            playerTired += value;

            if (playerTired >= maxTired)
            {
                playerTired = maxTired;
            }
        }

        if (vitalType == "Cold")
        {
            playerCold += value;

            if (playerCold >= maxCold)
            {
                playerCold = maxCold;
            }
        }

        if (vitalType == "Normal")
        {
            mentalBar.fillAmount = playerHealth / maxHealth;
            UpdateMentalBarColor(); // Update color based on new fillAmount
        }
    }

    public void UpdateHealth(string healthItemType, int value)
    {
        if (healthItemType == "Suffer")
        {
            if (playerHealth > 0)
            {
                playerHealth -= value / (hungerTolerance + tiredTolerance + coldTolerance);
                mentalBar.fillAmount = playerHealth / maxHealth;
                UpdateMentalBarColor(); // Update color immediately
            }
        }

        if (healthItemType == "Damage")
        {
            if (playerHealth > 0)
            {
                playerHealth -= value;
                mentalBar.fillAmount = playerHealth / maxHealth;
                UpdateMentalBarColor(); // Update color immediately
            }

            if (playerHealth <= maxHealth)
            {
                //death
            }
        }

        if (healthItemType == "Recover")
        {
            if (playerHealth > 0)
            {
                playerHealth += value;
                mentalBar.fillAmount = playerHealth / maxHealth;
                UpdateMentalBarColor(); // Update color immediately

                if (playerHealth >= maxHealth)
                {
                    playerHealth = maxHealth;
                }
            }
        }
    }

    private void UpdateMentalBarColor()
    {
        if (isColorOverridden) return;

        float fillRatio = mentalBar.fillAmount;
        Color green = new Color(0f, 1f, 0f, 1f);     // Pure GREEN (full)
        Color yellow = new Color(1f, 1f, 0f, 1f);    // Pure YELLOW (half)
        Color red = new Color(1f, 0f, 0f, 1f);       // Pure RED (empty)

        Color barColor;

        if (fillRatio > 0.5f)
        {
            // 0.51.0: YELLOW  GREEN
            float t = (fillRatio - 0.5f) * 2f;        // t: 01
            barColor = Color.Lerp(yellow, green, t);
        }
        else
        {
            // 0.00.5: RED  YELLOW  
            float t = fillRatio * 2f;                 // t: 01
            barColor = Color.Lerp(red, yellow, t);
        }


        mentalBar.color = barColor;
        playerImage.color = barColor; // Also update player image
    }

    // Keep original method but simplified (no longer needed for auto-coloring)
    public void ChangeMentalBarColor(Color newColor)
    {
        StopAllCoroutines();
        StartCoroutine(ChangeColorRoutine(newColor));
    }

    private IEnumerator ChangeColorRoutine(Color newColor)
    {
        isColorOverridden = true;

        // FIRST: Calculate what the automatic color SHOULD be
        float fillRatio = mentalBar.fillAmount;
        Color targetColor = Color.Lerp(Color.red, Color.green, fillRatio);

        // INSTANTLY change to new color
        mentalBar.color = newColor;
        playerImage.color = newColor;

        yield return new WaitForSeconds(2f);

        // Smoothly transition BACK to automatic color
        float elapsed = 0f;
        float duration = 0.5f;
        Color currentColor = newColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            mentalBar.color = Color.Lerp(currentColor, targetColor, t);
            playerImage.color = Color.Lerp(currentColor, targetColor, t);
            yield return null;
        }

        // Snap to exact final color
        mentalBar.color = targetColor;
        playerImage.color = targetColor;

        isColorOverridden = false;
    }
}
