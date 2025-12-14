using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private FadeBlackScreen fadeScript;


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
            } else
            {
                playerCold = maxCold;
            }
        }

        UpdateVitals("Normal", 0);

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
            }
        }

        if (healthItemType == "Damage")
        {
            if (playerHealth > 0)
            {
                playerHealth -= value;
                mentalBar.fillAmount = playerHealth / maxHealth;
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

                if (playerHealth >= maxHealth)
                {
                    playerHealth = maxHealth;
                }
            }
        }
    }

    //change colour
   public void ChangeMentalBarColor(Color newColor)
    {
        StopAllCoroutines(); // stop previous color changes if active
        StartCoroutine(ChangeColorRoutine(newColor));
    }

    private IEnumerator ChangeColorRoutine(Color newColor)
    {
        //Color originalColor = mentalBar.color;  // store original (usually white)
        mentalBar.color = newColor;             // change to the desired color
        yield return new WaitForSeconds(2f);    // wait for 2 seconds
        mentalBar.color = Color.white;        // revert to original
    }

}
