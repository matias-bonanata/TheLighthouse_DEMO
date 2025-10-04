using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Inventory")]
    private int maxWood = 45;
    public int availableWood;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI uiText;
    public PlayerInteractUI playerInteractUI;

    [Header("Wood Object to Scale")]
    [SerializeField] private Transform UIObject;
    private Renderer UIObjectRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        availableWood = maxWood;
        UpdateInventoryText();
        UpdateUIObjectScale();

        if (UIObjectRenderer != null)
        {
            UIObjectRenderer = UIObject.GetComponent<SpriteRenderer>();
            UIObjectRenderer.material = new Material(UIObjectRenderer.material);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void UpdateInventoryText()
    {
        if (uiText != null)
        {
            uiText.text = $"Firewood Available: {availableWood} / {maxWood}";
        }
    }

    void UpdateUIObjectScale()
    {
        if (UIObject != null)
        {
            // Calculate scale factor from 0 (no wood) to 1 (full wood)
            float scaleY = Mathf.Clamp01((float)availableWood / maxWood) * 0.47f;
            Vector3 scale = UIObject.localScale;
            scale.y = scaleY;
            UIObject.localScale = scale;

            //if (UIObjectRenderer != null)
            //{
            //    UIObjectRenderer.material.color = Color.Lerp(Color.red, Color.green, availableWood/maxWood);
            //}
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerInteractUI != null)
            {
                playerInteractUI.ShowContainer();
                UpdateInventoryText();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (availableWood > 0)
                {
                    availableWood--;
                    UpdateInventoryText();
                    UpdateUIObjectScale();
                }
                else
                {
                    uiText.text = "No more Wood available";
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerInteractUI != null)
                playerInteractUI.HideContainer();
        }
    }
}
