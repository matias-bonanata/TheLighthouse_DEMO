using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrabCatcher : MonoBehaviour
{
    [Header("Crab Settings")]
    [SerializeField] private int crabAmount = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text crabAmountText;
    [SerializeField] private GameObject canvasObjectToActivate;
    [SerializeField] private GameObject MenuContainer;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button throwAwayButton;

    [Header("Crab Spawning")]
    [SerializeField] private CrabSpawner crabSpawner;  // Reference to the GameObject with crab array
    [SerializeField] private int objectsToSpawnPerCrab = 1;  // How many objects per crab

    [Header("Timing (seconds)")]
    [SerializeField] private float minInterval = 10f;
    [SerializeField] private float maxInterval = 20f;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private bool playerInTrigger = false;

    private void Start()
    {
        UpdateCrabText();
        ScheduleNextIncrease();

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetCrabAmount);
        }

        if (throwAwayButton != null)
        {
            throwAwayButton.onClick.AddListener(ThrowAwayCrabs);
        }
    }

    private void Update()
    {
        if (playerInTrigger && Input.GetKeyDown(interactionKey))
        {
            if (canvasObjectToActivate != null)
            {
                canvasObjectToActivate.SetActive(true);
            }
        }
    }

    private void ResetCrabAmount()
    {
        // Spawn crabs first, THEN reset counter
        if (crabSpawner != null)
        {
            int totalObjectsToSpawn = crabAmount * objectsToSpawnPerCrab;
            crabSpawner.SpawnCrabs(totalObjectsToSpawn);
        }

        crabAmount = 0;
        UpdateCrabText();
    }

    private void ThrowAwayCrabs()
    {
        crabAmount = 0;
        UpdateCrabText();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
        }

        if (MenuContainer != null)
        {
            MenuContainer.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
        }

        if (canvasObjectToActivate != null)
        {
            canvasObjectToActivate.SetActive(false);
        }

        if (MenuContainer != null)
        {
            MenuContainer.SetActive(false);
        }
    }

    private void ScheduleNextIncrease()
    {
        float delay = Random.Range(minInterval, maxInterval);
        Invoke(nameof(IncreaseCrabAmount), delay);
    }

    private void IncreaseCrabAmount()
    {
        crabAmount += 1;
        UpdateCrabText();
        ScheduleNextIncrease();
    }

    private void UpdateCrabText()
    {
        if (crabAmountText != null)
        {
            crabAmountText.text = $"{crabAmount}x";
        }
    }
}
