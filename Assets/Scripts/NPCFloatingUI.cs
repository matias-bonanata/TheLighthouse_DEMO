using UnityEngine;
using UnityEngine.UI;

public class NPCFloatingUI : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject hoverObject;      // The floating visual above the NPC
    [SerializeField] private SpriteRenderer hoverImage;            // Reference to the Image component (optional, for color manipulation)

    //ui activation and scale
    [SerializeField] private float activationDistance = 3f;
    [SerializeField] private float awayScale = 0.5f;
    private bool isPlayerColliding = false;
    
    private void Update()
    {
        // Distance check
        float dist = Vector3.Distance(player.transform.position, transform.position);
        bool shouldShow = dist < activationDistance;

        // Activate/deactivate hovering object based on distance
        hoverObject.SetActive(shouldShow);

        if (shouldShow)
        {
            // Set visual state based on collision
            if (isPlayerColliding)
            {
                SetImageColor(Color.white, 1f);
                hoverObject.transform.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                Color grayHalfTransparent = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grayscale, alpha 50%
                SetImageColor(grayHalfTransparent, 0.5f);
                hoverObject.transform.localScale = new Vector3((awayScale / dist), 
                    awayScale / dist, awayScale / dist);
            }
        }


    }

    // Collider methods
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerColliding = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerColliding = false;
        }
    }

    // Helper to set image color and alpha
    private void SetImageColor(Color color, float alpha)
    {
        if (hoverImage != null)
        {
            color.a = alpha;
            hoverImage.color = color;
        }
    }
}
